namespace FingerScreensaver
{
    /// <summary>
    /// Представляет одну частицу в эффекте звездного тунеля
    /// </summary>
    public class Particle
    {
        private static readonly Random Random = new Random();

        // 3D позиция и скорость
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; } // Глубина в туннеле (0 - близко, TunnelDepth - далеко)
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public float VelocityZ { get; set; }
        
        // Вращение
        public float Rotation { get; set; }
        public float RotationSpeed { get; set; }
        public float YRotation { get; set; } // Вращение вокруг оси Y
        public float YRotationSpeed { get; set; } // Скорость вращения вокруг оси Y
        
        // Система возраста и жизненного цикла
        public float Age { get; set; }
        public float MaxAge { get; set; }
        public float SpawnDelay { get; set; }
        public float FadeInDuration { get; set; }
        public float FadeOutDuration { get; set; }
        
        // Масштаб и визуальные параметры
        public float Scale { get; set; }
        
        // Кэшированные значения для оптимизации
        private float cachedPerspective = -1f;
        private float cachedZ = -1f;
        
        // Константы для туннеля
        public const float TunnelDepth = 1000f;
        public const float FOV = 400f;
        
        // Константы для вращения (градусы в секунду)
        private const float MinRotationSpeed = -30f; // Минимальная скорость вращения
        private const float MaxRotationSpeed = 30f;   // Максимальная скорость вращения
        private const float MinYRotationSpeed = -60f; // Минимальная скорость вращения вокруг Y
        private const float MaxYRotationSpeed = 60f;  // Максимальная скорость вращения вокруг Y

        public Particle(int centerX, int centerY)
        {
            Reset(centerX, centerY);
        }

        /// <summary>
        /// Сбрасывает частицу в начальное положение (по окружности в глубине туннеля)
        /// </summary>
        public void Reset(int centerX, int centerY)
        {
            // Случайный угол и радиус для распределения по окружности
            double angle = Random.NextDouble() * Math.PI * 2;
            float radius = (float)(Random.NextDouble() * 800 + 200); // 200-1000 пикселей от центра
            
            // Начальная позиция - окружность в глубине туннеля
            X = (float)Math.Cos(angle) * radius + ((float)Random.NextDouble() - 0.5f) * 200;
            Y = (float)Math.Sin(angle) * radius + ((float)Random.NextDouble() - 0.5f) * 200;
            Z = (float)Random.NextDouble() * TunnelDepth; // Случайная глубина
            
            // Скорость движения к центру с вариацией (еще больше уменьшена)
            VelocityX = (float)-Math.Cos(angle) * 0.2f + ((float)Random.NextDouble() - 0.5f) * 0.4f;
            VelocityY = (float)-Math.Sin(angle) * 0.2f + ((float)Random.NextDouble() - 0.5f) * 0.4f;
            VelocityZ = (float)-(Random.NextDouble() * 3 + 1); // От -1 до -4 (еще больше уменьшена скорость движения вперед)
            
            // Вращение
            Rotation = (float)(Random.NextDouble() * 360);
            RotationSpeed = MinRotationSpeed + (float)(Random.NextDouble() * (MaxRotationSpeed - MinRotationSpeed));
            
            // Вращение вокруг оси Y (заметное и рандомное)
            YRotation = (float)(Random.NextDouble() * 360);
            YRotationSpeed = MinYRotationSpeed + (float)(Random.NextDouble() * (MaxYRotationSpeed - MinYRotationSpeed));
            
            // Масштаб
            Scale = 0.5f + (float)Random.NextDouble() * 0.5f; // 0.5-1.0
            
            // Жизненный цикл
            Age = 0;
            MaxAge = 6 + (float)Random.NextDouble() * 6; // 6-12 секунд
            SpawnDelay = (float)Random.NextDouble() * 2; // 0-2 секунды задержки
            FadeInDuration = 1.0f; // 1 секунда на появление
            FadeOutDuration = 1.5f; // 1.5 секунды на исчезновение
        }

        /// <summary>
        /// Обновляет позицию и параметры частицы
        /// </summary>
        public void Update(float deltaTime, int centerX, int centerY)
        {
            // Обновляем возраст
            Age += deltaTime;
            
            // Сбрасываем частицу, если она прожила максимальный возраст или вышла из туннеля
            if (Age >= MaxAge + SpawnDelay || Z <= 0)
            {
                Reset(centerX, centerY);
                return;
            }
            
            // Обновляем позицию
            X += VelocityX * deltaTime;
            Y += VelocityY * deltaTime;
            Z += VelocityZ * deltaTime * 60; // * 60 для согласования со скоростью из tunnel.js
            
            // Обновляем вращение
            Rotation += RotationSpeed * deltaTime;
            YRotation += YRotationSpeed * deltaTime;
            
            // Нормализуем углы более эффективно
            Rotation = Rotation % 360f;
            if (Rotation < 0) Rotation += 360f;
            
            YRotation = YRotation % 360f;
            if (YRotation < 0) YRotation += 360f;
        }

        /// <summary>
        /// Проверяет, находится ли частица за пределами экрана (не используется с новой системой возраста)
        /// </summary>
        public bool IsOutOfBounds(int width, int height)
        {
            // С новой системой частицы управляются через возраст, а не через границы
            return false;
        }

        /// <summary>
        /// Вычисляет 3D проекцию и возвращает размер на экране
        /// </summary>
        public float GetScreenSize(int centerX, int centerY, out float screenX, out float screenY)
        {
            if (Z <= 0)
            {
                screenX = centerX;
                screenY = centerY;
                return 0;
            }
            
            // Кэширование perspective для оптимизации
            if (Math.Abs(cachedZ - Z) > 0.1f) // Если Z изменилось значительно
            {
                cachedZ = Z;
                cachedPerspective = FOV / Math.Max(Z, 0.1f);
            }
            
            // 3D проекция с кэшированным значением
            float perspective = cachedPerspective;
            
            // Ограничиваем значения для предотвращения переполнения
            float projectedX = X * perspective;
            float projectedY = Y * perspective;
            
            // Ограничиваем проекцию разумными пределами
            projectedX = Math.Max(-10000f, Math.Min(10000f, projectedX));
            projectedY = Math.Max(-10000f, Math.Min(10000f, projectedY));
            
            screenX = centerX + projectedX;
            screenY = centerY + projectedY;
            
            // Размер на экране
            float baseSize = 60;
            float screenScale = perspective * Scale;
            return Math.Max(16, Math.Min(1000, baseSize * screenScale)); // Ограничиваем размер
        }

        /// <summary>
        /// Возвращает прозрачность частицы с учетом жизненного цикла, расстояния и границ экрана
        /// </summary>
        public float GetOpacity(float screenScale, int screenWidth, int screenHeight, float screenX, float screenY)
        {
            // Вычисляем lifecycle alpha
            float lifecycleAlpha = CalculateLifecycleAlpha();
            
            // Альфа от расстояния (ближе = ярче)
            float distanceAlpha = Math.Min(1.0f, 0.2f + screenScale * 0.8f);
            
            // Альфа от границ экрана (исчезание при приближении к краям)
            float edgeAlpha = CalculateEdgeAlpha(screenWidth, screenHeight, screenX, screenY);
            
            // Комбинируем все факторы
            return distanceAlpha * lifecycleAlpha * edgeAlpha;
        }
        
        /// <summary>
        /// Вычисляет альфа-канал на основе жизненного цикла с easing
        /// </summary>
        private float CalculateLifecycleAlpha()
        {
            // Если еще не началось появление
            if (Age < SpawnDelay)
                return 0;
            
            float adjustedAge = Age - SpawnDelay;
            
            // Fade In с cubic easing
            if (adjustedAge < FadeInDuration)
            {
                float progress = adjustedAge / FadeInDuration;
                float easedProgress = 1 - (float)Math.Pow(1 - progress, 3); // cubic ease out
                return easedProgress;
            }
            // Fade Out с quadratic easing
            else if (adjustedAge > MaxAge - FadeOutDuration)
            {
                float fadeOutProgress = (MaxAge - adjustedAge) / FadeOutDuration;
                float easedProgress = (float)Math.Pow(fadeOutProgress, 2); // quadratic
                return Math.Max(0, easedProgress);
            }
            // Полная видимость
            else
            {
                return 1.0f;
            }
        }
        
        /// <summary>
        /// Вычисляет альфа-канал на основе расстояния до границ экрана
        /// </summary>
        private float CalculateEdgeAlpha(int screenWidth, int screenHeight, float screenX, float screenY)
        {
            // Определяем зону исчезания (в пикселях от края экрана) - увеличена для лучшей видимости
            float fadeZone = 400f; // Зона исчезания 400 пикселей (увеличена с 200)
            
            // Вычисляем расстояния до каждого края
            float distanceToLeft = screenX;
            float distanceToRight = screenWidth - screenX;
            float distanceToTop = screenY;
            float distanceToBottom = screenHeight - screenY;
            
            // Находим минимальное расстояние до любого края
            float minDistanceToEdge = Math.Min(Math.Min(distanceToLeft, distanceToRight), 
                                              Math.Min(distanceToTop, distanceToBottom));
            
            // Если частица далеко от краев, она полностью видима
            if (minDistanceToEdge >= fadeZone)
                return 1.0f;
            
            // Если частица за пределами экрана, она невидима
            if (minDistanceToEdge <= 0)
                return 0.0f;
            
            // Плавное исчезание в зоне fadeZone с более резким переходом
            float fadeProgress = minDistanceToEdge / fadeZone;
            // Используем квадратичную функцию для более заметного затухания
            return fadeProgress * fadeProgress;
        }
        
        /// <summary>
        /// Вычисляет масштабный коэффициент для плавного появления и исчезновения
        /// </summary>
        public float GetLifecycleScale()
        {
            if (Age < SpawnDelay)
                return 0.1f;
            
            float adjustedAge = Age - SpawnDelay;
            
            // Плавное появление без bounce эффекта
            if (adjustedAge < FadeInDuration)
            {
                float progress = adjustedAge / FadeInDuration;
                float easeProgress = 1 - (float)Math.Pow(1 - progress, 3);
                
                return 0.1f + (0.9f * easeProgress);
            }
            // Shrink при исчезновении
            else if (adjustedAge > MaxAge - FadeOutDuration)
            {
                float fadeOutProgress = (MaxAge - adjustedAge) / FadeOutDuration;
                float easeProgress = (float)Math.Pow(fadeOutProgress, 1.5);
                return 0.1f + (0.9f * easeProgress);
            }
            // Стабильный размер в середине жизни
            else
            {
                return 1.0f;
            }
        }
    }
}

