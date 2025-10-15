using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.IO;

namespace FingerScreensaver
{
    /// <summary>
    /// Главная форма скринсейвера с эффектом звездного тунеля
    /// </summary>
    public class ScreensaverForm : Form
    {
        private readonly List<Particle> particles = new List<Particle>();
        private readonly System.Windows.Forms.Timer animationTimer;
        private readonly Image fingerImage;
        private Point lastMousePosition;
        private bool isPreviewMode;
        private int centerX;
        private int centerY;
        
        // Для расчета deltaTime
        private DateTime lastUpdateTime;
        private const int ParticleCount = 100; // Увеличено с 75 до 100
        
        // Оптимизация: кэшированный отсортированный список частиц
        private readonly List<Particle> sortedParticles = new List<Particle>();
        private bool needsSorting = true;

        public ScreensaverForm(bool previewMode = false)
        {
            isPreviewMode = previewMode;

            // Загружаем изображение пальца из ресурсов
            fingerImage = LoadFingerImage();

            // Настройка формы
            if (!previewMode)
            {
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                TopMost = true;
                Cursor.Hide();
            }
            else
            {
                FormBorderStyle = FormBorderStyle.None;
                Size = new Size(320, 240);
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                Location = new Point(0, 0);
            }

            BackColor = Color.Black;
            DoubleBuffered = true;

            // Инициализация центра экрана
            centerX = ClientSize.Width / 2;
            centerY = ClientSize.Height / 2;

            // Создаем частицы
            for (int i = 0; i < ParticleCount; i++)
            {
                particles.Add(new Particle(centerX, centerY));
            }

            // Таймер для анимации (~60 FPS)
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 16; // ~60 FPS
            animationTimer.Tick += AnimationTimer_Tick;
            
            // Инициализируем время
            lastUpdateTime = DateTime.Now;
            animationTimer.Start();

            // События для выхода
            if (!previewMode)
            {
                KeyDown += ScreensaverForm_KeyDown;
                MouseMove += ScreensaverForm_MouseMove;
                MouseClick += ScreensaverForm_MouseClick;
                lastMousePosition = Cursor.Position;
            }

            // Обработка изменения размера окна
            Resize += ScreensaverForm_Resize;
            
            // Обработка закрытия окна
            FormClosing += ScreensaverForm_FormClosing;
        }

        private void ScreensaverForm_Resize(object? sender, EventArgs e)
        {
            centerX = ClientSize.Width / 2;
            centerY = ClientSize.Height / 2;
        }

        private void ScreensaverForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Останавливаем таймер при закрытии окна
            animationTimer?.Stop();
        }

        private Image LoadFingerImage()
        {
            // Для Native AOT используем прямой путь к файлу
            // В production сборке изображение будет встроено как ресурс
            try
            {
                // Попробуем загрузить из встроенных ресурсов (совместимо с Native AOT)
                var assembly = typeof(ScreensaverForm).Assembly;
                var resourceName = "FingerScreensaver.finger.png";
                
                using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        return Image.FromStream(stream);
                    }
                }
            }
            catch
            {
                // Fallback: попробуем загрузить из файла в той же директории
                try
                {
                    string currentDir = Path.GetDirectoryName(Environment.ProcessPath) ?? "";
                    string imagePath = Path.Combine(currentDir, "finger.png");
                    if (File.Exists(imagePath))
                    {
                        return Image.FromFile(imagePath);
                    }
                }
                catch
                {
                    // Если ничего не работает, создаем простое изображение программно
                }
            }
            
            // Fallback: создаем простое изображение программно
            return CreateFallbackImage();
        }
        
        private Image CreateFallbackImage()
        {
            // Создаем простое изображение пальца программно
            Bitmap bitmap = new Bitmap(64, 64);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.None; // Убираем сглаживание
                g.Clear(Color.Transparent);
                
                // Рисуем простой палец
                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    // Основная часть пальца
                    g.FillEllipse(brush, 20, 10, 24, 40);
                    
                    // Кончик пальца
                    g.FillEllipse(brush, 22, 5, 20, 15);
                    
                    // Ноготь
                    using (SolidBrush nailBrush = new SolidBrush(Color.LightGray))
                    {
                        g.FillEllipse(nailBrush, 24, 7, 16, 8);
                    }
                }
            }
            return bitmap;
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            // Вычисляем deltaTime
            DateTime currentTime = DateTime.Now;
            float deltaTime = (float)(currentTime - lastUpdateTime).TotalSeconds;
            lastUpdateTime = currentTime;
            
            // Ограничиваем deltaTime для стабильности (на случай лагов)
            deltaTime = Math.Min(deltaTime, 0.1f);
            
            // Обновляем все частицы
            foreach (var particle in particles)
            {
                particle.Update(deltaTime, centerX, centerY);
            }
            
            // Помечаем необходимость сортировки
            needsSorting = true;

            // Перерисовываем
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None; // Убираем сглаживание для устранения эффекта свечения
            g.InterpolationMode = InterpolationMode.Bilinear;

            // Оптимизированная сортировка частиц по глубине
            if (needsSorting)
            {
                sortedParticles.Clear();
                sortedParticles.AddRange(particles);
                sortedParticles.Sort((p1, p2) => p2.Z.CompareTo(p1.Z)); // Сортировка по убыванию Z
                needsSorting = false;
            }

            foreach (var particle in sortedParticles)
            {
                // Вычисляем 3D проекцию и размер на экране
                float size = particle.GetScreenSize(centerX, centerY, out float screenX, out float screenY);
                
                if (size <= 0) continue;
                
                // Вычисляем масштаб для расчета прозрачности
                float perspective = Particle.FOV / Math.Max(0.1f, particle.Z);
                float screenScale = perspective * particle.Scale;
                
                // Получаем прозрачность с учетом жизненного цикла и границ экрана
                float opacity = particle.GetOpacity(screenScale, ClientSize.Width, ClientSize.Height, screenX, screenY);

                if (opacity <= 0) continue;

                // Получаем lifecycle scale для breathing/bounce эффектов
                float lifecycleScale = particle.GetLifecycleScale();

                // Создаем матрицу для трансформации (поворот и масштабирование)
                GraphicsState state = g.Save();
                
                g.TranslateTransform(screenX, screenY);
                
                // Ограничиваем углы вращения для предотвращения переполнения
                float totalRotation = (particle.Rotation + particle.YRotation) % 360f;
                if (totalRotation < 0) totalRotation += 360f;
                
                g.RotateTransform(totalRotation);
                g.ScaleTransform(lifecycleScale, lifecycleScale);

                // Применяем прозрачность
                ColorMatrix colorMatrix = new ColorMatrix();
                colorMatrix.Matrix33 = opacity; // Alpha
                ImageAttributes imageAttributes = new ImageAttributes();
                imageAttributes.SetColorMatrix(colorMatrix);

                // Рисуем изображение пальца
                float halfSize = size / 2;
                Rectangle destRect = new Rectangle(
                    (int)-halfSize, 
                    (int)-halfSize, 
                    (int)size, 
                    (int)size
                );

                g.DrawImage(
                    fingerImage,
                    destRect,
                    0, 0, fingerImage.Width, fingerImage.Height,
                    GraphicsUnit.Pixel,
                    imageAttributes
                );

                g.Restore(state);
            }
        }

        private void ScreensaverForm_KeyDown(object? sender, KeyEventArgs e)
        {
            ExitScreensaver();
        }

        private void ScreensaverForm_MouseMove(object? sender, MouseEventArgs e)
        {
            // Выходим только если мышь реально двигалась (не первое срабатывание)
            Point currentPosition = Cursor.Position;
            if (Math.Abs(currentPosition.X - lastMousePosition.X) > 10 ||
                Math.Abs(currentPosition.Y - lastMousePosition.Y) > 10)
            {
                ExitScreensaver();
            }
        }

        private void ScreensaverForm_MouseClick(object? sender, MouseEventArgs e)
        {
            ExitScreensaver();
        }

        private void ExitScreensaver()
        {
            if (!isPreviewMode)
            {
                Cursor.Show();
                animationTimer.Stop();
                Close();
            }
            else
            {
                // В режиме предварительного просмотра тоже закрываем окно
                animationTimer.Stop();
                Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer?.Dispose();
                fingerImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
