// Finger Tunnel Screensaver - Native Integration Version
// Optimized for macOS screensaver with native settings integration

class FingerTunnel {
    constructor() {
        this.canvas = document.getElementById('tunnel-canvas');
        this.ctx = this.canvas.getContext('2d');
        
        // Performance optimizations
        this.ctx.imageSmoothingEnabled = false;
        
        // Configuration - will be updated by native code
        this.config = {
            particleCount: 100,
            speed: 1.0,
            tunnelDepth: 1000,
            fov: 400,
            centerX: 0,
            centerY: 0,
            smoothTransitions: true,
            enhancedEffects: true
        };
        
        // Performance settings
        this.performance = {
            targetFPS: 60,
            minFPS: 30,
            adaptiveQuality: true,
            frameSkip: 1,
            currentFrameSkip: 1,
            lowPerformanceMode: false
        };
        
        // Particle system
        this.particles = [];
        this.visibleParticles = [];
        this.fingerImage = null;
        this.imageLoaded = false;
        
        // Performance tracking
        this.lastTime = 0;
        this.frameCount = 0;
        this.fps = 60;
        this.fpsHistory = [];
        
        // Animation state
        this.isRunning = false;
        this.animationId = null;
        
        // Culling bounds cache
        this.cullingBounds = {
            left: 0,
            right: 0,
            top: 0,
            bottom: 0,
            margin: 100
        };
        
        this.init();
    }
    
    init() {
        this.setupCanvas();
        this.loadImage();
    }
    
    loadImage() {
        this.fingerImage = new Image();
        this.fingerImage.onload = () => {
            this.imageLoaded = true;
            this.createParticles();
            this.start();
            console.log('Finger image loaded successfully');
        };
        this.fingerImage.onerror = () => {
            console.error('Failed to load finger image');
            this.imageLoaded = false;
            // Create fallback particles without image
            this.createParticles();
            this.start();
        };
        // Use relative path for screensaver bundle
        this.fingerImage.src = 'finger.png';
    }
    
    setupCanvas() {
        const updateCanvasSize = () => {
            const rect = this.canvas.getBoundingClientRect();
            
            // Adaptive DPR for performance
            let dpr = window.devicePixelRatio || 1;
            
            // Use screen info if available from native code
            if (this.screenInfo) {
                dpr = this.screenInfo.scaleFactor || dpr;
                console.log(`Using native screen scale factor: ${dpr}`);
            }
            
            // Reduce DPR on large screens for better performance
            const screenArea = rect.width * rect.height;
            if (screenArea > 2073600) { // 1920x1080
                dpr = Math.min(dpr, 1.5);
            }
            if (screenArea > 8294400) { // 4K
                dpr = Math.min(dpr, 1.0);
            }
            
            this.canvas.width = rect.width * dpr;
            this.canvas.height = rect.height * dpr;
            
            this.ctx.scale(dpr, dpr);
            this.ctx.imageSmoothingEnabled = false;
            this.canvas.style.width = rect.width + 'px';
            this.canvas.style.height = rect.height + 'px';
            
            // Update center coordinates
            this.config.centerX = rect.width / 2;
            this.config.centerY = rect.height / 2;
            
            // Update culling bounds
            this.updateCullingBounds();
            
            // Adapt particle count based on screen size
            this.adaptParticleCount(screenArea);
        };
        
        updateCanvasSize();
        window.addEventListener('resize', updateCanvasSize);
    }
    
    updateCullingBounds() {
        const margin = this.cullingBounds.margin;
        this.cullingBounds.left = -margin;
        this.cullingBounds.right = this.canvas.width / (window.devicePixelRatio || 1) + margin;
        this.cullingBounds.top = -margin;
        this.cullingBounds.bottom = this.canvas.height / (window.devicePixelRatio || 1) + margin;
    }
    
    adaptParticleCount(screenArea) {
        if (!this.performance.adaptiveQuality) return;
        
        let targetCount = this.config.particleCount;
        
        // Reduce particles on large screens
        if (screenArea > 2073600) { // 1920x1080
            targetCount = Math.floor(this.config.particleCount * 0.7);
        }
        if (screenArea > 8294400) { // 4K
            targetCount = Math.floor(this.config.particleCount * 0.5);
        }
        
        // Update particles if count changed significantly
        if (Math.abs(this.particles.length - targetCount) > 10) {
            this.config.particleCount = targetCount;
            this.createParticles();
        }
    }
    
    createParticles() {
        this.particles = [];
        
        for (let i = 0; i < this.config.particleCount; i++) {
            this.particles.push(this.createParticle());
        }
    }
    
    createParticle() {
        const angle = Math.random() * Math.PI * 2;
        const radius = Math.random() * 800 + 200;
        
        return {
            x: Math.cos(angle) * radius + (Math.random() - 0.5) * 200,
            y: Math.sin(angle) * radius + (Math.random() - 0.5) * 200,
            z: Math.random() * this.config.tunnelDepth,
            
            vx: -Math.cos(angle) * 0.5 + (Math.random() - 0.5) * 1,
            vy: -Math.sin(angle) * 0.5 + (Math.random() - 0.5) * 1,
            vz: -Math.random() * 8 - 3,
            
            rotation: Math.random() * Math.PI * 2,
            rotationSpeed: (Math.random() - 0.5) * 0.1,
            scale: 0.5 + Math.random() * 0.5,
            
            age: 0,
            maxAge: 6 + Math.random() * 6,
            fadeInDuration: 1.0,
            fadeOutDuration: 1.5,
            lifecycleAlpha: 0,
            spawnDelay: Math.random() * 2,
            
            screenX: 0,
            screenY: 0,
            screenScale: 1,
            visible: true
        };
    }
    
    updateParticle(particle, deltaTime) {
        particle.age += deltaTime;
        
        // Calculate lifecycle alpha for smooth fade in/out
        if (this.config.smoothTransitions && !this.performance.lowPerformanceMode) {
            if (particle.age < particle.spawnDelay) {
                particle.lifecycleAlpha = 0;
            } else {
                const adjustedAge = particle.age - particle.spawnDelay;
                
                if (adjustedAge < particle.fadeInDuration) {
                    const progress = adjustedAge / particle.fadeInDuration;
                    const easedProgress = 1 - Math.pow(1 - progress, 3);
                    particle.lifecycleAlpha = easedProgress;
                } else if (adjustedAge > particle.maxAge - particle.fadeOutDuration) {
                    const fadeOutProgress = (particle.maxAge - adjustedAge) / particle.fadeOutDuration;
                    const easedProgress = Math.pow(fadeOutProgress, 2);
                    particle.lifecycleAlpha = Math.max(0, easedProgress);
                } else {
                    particle.lifecycleAlpha = 1;
                }
            }
        } else {
            particle.lifecycleAlpha = particle.age >= particle.spawnDelay ? 1 : 0;
        }
        
        // Reset particle if needed
        if (particle.age >= particle.maxAge + particle.spawnDelay || particle.z <= 0) {
            this.resetParticle(particle);
        }
        
        // Update 3D position
        particle.x += particle.vx * deltaTime * this.config.speed;
        particle.y += particle.vy * deltaTime * this.config.speed;
        particle.z += particle.vz * deltaTime * this.config.speed * 60;
        
        // Update rotation
        particle.rotation += particle.rotationSpeed * deltaTime * 60;
        
        // Project 3D to 2D
        this.projectParticle(particle);
    }
    
    resetParticle(particle) {
        const angle = Math.random() * Math.PI * 2;
        const radius = Math.random() * 800 + 200;
        
        particle.x = Math.cos(angle) * radius + (Math.random() - 0.5) * 200;
        particle.y = Math.sin(angle) * radius + (Math.random() - 0.5) * 200;
        particle.z = this.config.tunnelDepth;
        particle.vx = -Math.cos(angle) * 0.5 + (Math.random() - 0.5) * 1;
        particle.vy = -Math.sin(angle) * 0.5 + (Math.random() - 0.5) * 1;
        particle.vz = -Math.random() * 8 - 3;
        
        particle.age = 0;
        particle.maxAge = 6 + Math.random() * 6;
        particle.lifecycleAlpha = 0;
        particle.spawnDelay = Math.random() * 2;
    }
    
    calculateLifecycleScale(particle) {
        if (particle.age < particle.spawnDelay) {
            return 0.1;
        }
        
        const adjustedAge = particle.age - particle.spawnDelay;
        
        if (adjustedAge < particle.fadeInDuration) {
            const progress = adjustedAge / particle.fadeInDuration;
            const easeProgress = 1 - Math.pow(1 - progress, 3);
            const bounceScale = progress > 0.7 ? 1 + (0.1 * Math.sin((progress - 0.7) * Math.PI * 3.33)) : 1;
            return (0.1 + (0.9 * easeProgress)) * bounceScale;
        } else if (adjustedAge > particle.maxAge - particle.fadeOutDuration) {
            const fadeOutProgress = (particle.maxAge - adjustedAge) / particle.fadeOutDuration;
            const easeProgress = Math.pow(fadeOutProgress, 1.5);
            return 0.1 + (0.9 * easeProgress);
        } else {
            const breathingEffect = 1 + (0.05 * Math.sin(adjustedAge * 2));
            return breathingEffect;
        }
    }
    
    projectParticle(particle) {
        if (particle.z <= 0) {
            particle.visible = false;
            return;
        }
        
        const perspective = this.config.fov / particle.z;
        
        particle.screenX = this.config.centerX + particle.x * perspective;
        particle.screenY = this.config.centerY + particle.y * perspective;
        particle.screenScale = perspective * particle.scale;
        
        particle.visible = (
            particle.screenX > this.cullingBounds.left &&
            particle.screenX < this.cullingBounds.right &&
            particle.screenY > this.cullingBounds.top &&
            particle.screenY < this.cullingBounds.bottom &&
            particle.screenScale > 0.01
        );
    }
    
    renderParticle(particle) {
        if (particle.lifecycleAlpha <= 0) return;
        
        const baseSize = 60;
        const size = Math.max(16, baseSize * particle.screenScale);
        
        if (this.performance.lowPerformanceMode && size < 20) return;
        
        this.ctx.save();
        
        this.ctx.translate(particle.screenX, particle.screenY);
        this.ctx.rotate(particle.rotation);
        
        if (this.config.smoothTransitions && !this.performance.lowPerformanceMode) {
            const lifecycleScale = this.calculateLifecycleScale(particle);
            this.ctx.scale(lifecycleScale, lifecycleScale);
        }
        
        const imageSize = Math.round(size);
        const distanceAlpha = Math.min(1, 0.2 + particle.screenScale * 0.8);
        let finalAlpha = distanceAlpha * particle.lifecycleAlpha;
        
        this.ctx.globalAlpha = finalAlpha;
        
        // Render image if loaded, otherwise render fallback
        if (this.imageLoaded && this.fingerImage) {
            this.ctx.drawImage(
                this.fingerImage,
                -imageSize / 2,
                -imageSize / 2,
                imageSize,
                imageSize
            );
        } else {
            // Fallback rendering - simple circle
            this.ctx.fillStyle = '#FFFFFF';
            this.ctx.beginPath();
            this.ctx.arc(0, 0, imageSize / 4, 0, Math.PI * 2);
            this.ctx.fill();
        }
        
        this.ctx.restore();
    }
    
    update(currentTime) {
        const deltaTime = (currentTime - this.lastTime) / 1000;
        this.lastTime = currentTime;
        
        this.updatePerformanceMetrics(deltaTime);
        
        this.frameCount++;
        if (this.frameCount % this.performance.currentFrameSkip !== 0) {
            return;
        }
        
        // Clear canvas
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        
        // Render tunnel background
        this.renderTunnelBackground();
        
        // Update particles
        this.particles.forEach(particle => {
            this.updateParticle(particle, deltaTime * this.performance.currentFrameSkip);
        });
        
        // Build visible particles list
        this.visibleParticles.length = 0;
        for (let i = 0; i < this.particles.length; i++) {
            const particle = this.particles[i];
            if (particle.visible) {
                this.visibleParticles.push(particle);
            }
        }
        
        // Sort by z-depth (far to near)
        this.visibleParticles.sort((a, b) => b.z - a.z);
        
        // Render visible particles
        for (let i = 0; i < this.visibleParticles.length; i++) {
            this.renderParticle(this.visibleParticles[i]);
        }
    }
    
    updatePerformanceMetrics(deltaTime) {
        const currentFPS = 1 / deltaTime;
        this.fpsHistory.push(currentFPS);
        
        if (this.fpsHistory.length > 60) {
            this.fpsHistory.shift();
        }
        
        if (this.frameCount % 30 === 0 && this.fpsHistory.length >= 30) {
            const avgFPS = this.fpsHistory.reduce((a, b) => a + b) / this.fpsHistory.length;
            this.fps = Math.round(avgFPS);
            
            if (this.performance.adaptiveQuality) {
                this.adaptPerformance(avgFPS);
            }
        }
    }
    
    adaptPerformance(avgFPS) {
        const wasLowPerf = this.performance.lowPerformanceMode;
        
        if (avgFPS < this.performance.minFPS) {
            this.performance.lowPerformanceMode = true;
            this.performance.currentFrameSkip = Math.min(3, this.performance.currentFrameSkip + 1);
            
            if (avgFPS < 20 && this.particles.length > 50) {
                this.config.particleCount = Math.max(50, this.particles.length - 20);
                this.createParticles();
            }
        } else if (avgFPS > this.performance.targetFPS * 0.8) {
            this.performance.lowPerformanceMode = false;
            this.performance.currentFrameSkip = Math.max(1, this.performance.currentFrameSkip - 1);
        }
        
        if (wasLowPerf !== this.performance.lowPerformanceMode) {
            console.log(`Performance mode: ${this.performance.lowPerformanceMode ? 'LOW' : 'NORMAL'} (FPS: ${Math.round(avgFPS)})`);
        }
    }
    
    renderTunnelBackground() {
        this.ctx.fillStyle = '#000000';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
    }
    
    animate(currentTime) {
        if (!this.isRunning) return;
        
        this.update(currentTime);
        this.animationId = requestAnimationFrame((time) => this.animate(time));
    }
    
    start() {
        if (this.isRunning) return;
        
        this.isRunning = true;
        this.lastTime = performance.now();
        this.animate(this.lastTime);
    }
    
    stop() {
        this.isRunning = false;
        if (this.animationId) {
            cancelAnimationFrame(this.animationId);
            this.animationId = null;
        }
    }
    
    // Public API methods for native integration
    setSpeed(speed) {
        const oldSpeed = this.config.speed;
        this.config.speed = Math.max(0.1, Math.min(5.0, speed)); // Clamp between 0.1 and 5.0
        
        if (oldSpeed !== this.config.speed) {
            console.log(`Speed updated: ${oldSpeed} -> ${this.config.speed}`);
        }
    }
    
    setParticleCount(count) {
        const oldCount = this.config.particleCount;
        
        // Validate particle count
        const validCounts = [50, 100, 200];
        if (!validCounts.includes(count)) {
            console.warn(`Invalid particle count ${count}, using default 100`);
            count = 100;
        }
        
        this.config.particleCount = count;
        
        if (oldCount !== count) {
            console.log(`Particle count updated: ${oldCount} -> ${count}`);
            this.createParticles();
            
            // Update culling bounds for new particle count
            this.updateCullingBounds();
        }
    }
    
    setPerformanceMode(mode) {
        const oldMode = {
            adaptiveQuality: this.performance.adaptiveQuality,
            lowPerformanceMode: this.performance.lowPerformanceMode,
            currentFrameSkip: this.performance.currentFrameSkip
        };
        
        switch(mode) {
            case 'high':
                this.performance.adaptiveQuality = false;
                this.performance.lowPerformanceMode = false;
                this.performance.currentFrameSkip = 1;
                this.performance.targetFPS = 60;
                break;
            case 'balanced':
            case 'auto':
                this.performance.adaptiveQuality = true;
                this.performance.lowPerformanceMode = false;
                this.performance.currentFrameSkip = 1;
                this.performance.targetFPS = 60;
                break;
            case 'low':
                this.performance.adaptiveQuality = false;
                this.performance.lowPerformanceMode = true;
                this.performance.currentFrameSkip = 2;
                this.performance.targetFPS = 30;
                break;
            default:
                console.warn(`Unknown performance mode: ${mode}, using balanced`);
                this.performance.adaptiveQuality = true;
                this.performance.lowPerformanceMode = false;
                this.performance.currentFrameSkip = 1;
                this.performance.targetFPS = 60;
                break;
        }
        
        // Log changes
        const newMode = {
            adaptiveQuality: this.performance.adaptiveQuality,
            lowPerformanceMode: this.performance.lowPerformanceMode,
            currentFrameSkip: this.performance.currentFrameSkip
        };
        
        if (JSON.stringify(oldMode) !== JSON.stringify(newMode)) {
            console.log(`Performance mode updated to ${mode}:`, newMode);
        }
    }
    
    setEffectsMode(mode) {
        const oldEffects = {
            smoothTransitions: this.config.smoothTransitions,
            enhancedEffects: this.config.enhancedEffects
        };
        
        switch(mode) {
            case 'minimal':
                this.config.smoothTransitions = false;
                this.config.enhancedEffects = false;
                break;
            case 'smooth':
                this.config.smoothTransitions = true;
                this.config.enhancedEffects = false;
                break;
            case 'enhanced':
                this.config.smoothTransitions = true;
                this.config.enhancedEffects = true;
                break;
            default:
                console.warn(`Unknown effects mode: ${mode}, using smooth`);
                this.config.smoothTransitions = true;
                this.config.enhancedEffects = false;
                break;
        }
        
        // Log changes
        const newEffects = {
            smoothTransitions: this.config.smoothTransitions,
            enhancedEffects: this.config.enhancedEffects
        };
        
        if (JSON.stringify(oldEffects) !== JSON.stringify(newEffects)) {
            console.log(`Effects mode updated to ${mode}:`, newEffects);
            
            // Recreate particles to apply new effects settings
            this.createParticles();
        }
    }
    
    getPerformanceInfo() {
        return {
            fps: this.fps,
            particleCount: this.particles.length,
            visibleParticles: this.visibleParticles.length,
            lowPerformanceMode: this.performance.lowPerformanceMode,
            frameSkip: this.performance.currentFrameSkip
        };
    }
    
    // Screen optimization methods
    optimizeForScreen(screenInfo) {
        if (!screenInfo) return;
        
        console.log('Optimizing FingerTunnel for screen:', screenInfo);
        
        // Store screen info
        this.screenInfo = Object.assign({}, screenInfo);
        
        // Update canvas setup for new screen
        this.setupCanvas();
        
        // Adjust performance settings
        this.adjustPerformanceForScreen(screenInfo);
        
        // Update particle system if needed
        this.updateParticleSystemForScreen(screenInfo);
    }
    
    adjustPerformanceForScreen(screenInfo) {
        const screenArea = screenInfo.width * screenInfo.height;
        const isHighRes = screenArea > 2073600; // > 1440p
        const isUltraHighRes = screenArea > 8294400; // > 4K
        
        // Adjust performance settings based on screen characteristics
        if (isUltraHighRes) {
            this.performance.targetFPS = 45;
            this.performance.adaptiveQuality = true;
            console.log('Ultra-high resolution detected, adjusting performance targets');
        } else if (isHighRes) {
            this.performance.targetFPS = 50;
            this.performance.adaptiveQuality = true;
            console.log('High resolution detected, adjusting performance targets');
        } else {
            this.performance.targetFPS = 60;
            this.performance.adaptiveQuality = true;
        }
        
        // Retina-specific optimizations
        if (screenInfo.isRetina) {
            // Enable enhanced effects for Retina displays if performance allows
            if (!isUltraHighRes) {
                this.config.enhancedEffects = true;
                console.log('Enabled enhanced effects for Retina display');
            }
        }
    }
    
    updateParticleSystemForScreen(screenInfo) {
        const screenArea = screenInfo.width * screenInfo.height;
        
        // Calculate optimal particle count for screen size
        let baseParticleCount = this.config.particleCount;
        let optimalCount = baseParticleCount;
        
        // Adjust based on screen area
        if (screenArea > 8294400) { // 4K+
            optimalCount = Math.floor(baseParticleCount * 0.6);
        } else if (screenArea > 4147200) { // 1440p+
            optimalCount = Math.floor(baseParticleCount * 0.75);
        } else if (screenArea > 2073600) { // 1080p+
            optimalCount = Math.floor(baseParticleCount * 0.9);
        }
        
        // Additional adjustment for Retina displays
        if (screenInfo.isRetina && screenArea > 2073600) {
            optimalCount = Math.floor(optimalCount * 0.8);
        }
        
        // Update particle count if significantly different
        if (Math.abs(this.particles.length - optimalCount) > 10) {
            console.log(`Updating particle count for screen: ${this.particles.length} → ${optimalCount}`);
            this.config.particleCount = optimalCount;
            this.createParticles();
        }
    }
    
    setTargetFPS(fps) {
        this.performance.targetFPS = Math.max(30, Math.min(60, fps));
        console.log(`Target FPS set to: ${this.performance.targetFPS}`);
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.fingerTunnel = new FingerTunnel();
    console.log('Finger Tunnel Screensaver initialized for native integration');
    
    // Register for settings changes if ScreensaverAPI is available
    if (window.ScreensaverAPI && window.ScreensaverAPI.addSettingsListener) {
        window.ScreensaverAPI.addSettingsListener(function(newSettings, previousSettings) {
            console.log('Settings changed:', {
                previous: previousSettings,
                current: newSettings
            });
            
            // Log specific changes
            if (newSettings.speed !== previousSettings.speed) {
                console.log(`Speed changed from ${previousSettings.speed} to ${newSettings.speed}`);
            }
            if (newSettings.particleCount !== previousSettings.particleCount) {
                console.log(`Particle count changed from ${previousSettings.particleCount} to ${newSettings.particleCount}`);
            }
            if (newSettings.performanceMode !== previousSettings.performanceMode) {
                console.log(`Performance mode changed from ${previousSettings.performanceMode} to ${newSettings.performanceMode}`);
            }
            if (newSettings.effectsMode !== previousSettings.effectsMode) {
                console.log(`Effects mode changed from ${previousSettings.effectsMode} to ${newSettings.effectsMode}`);
            }
        });
    }
});

// Handle page visibility changes
document.addEventListener('visibilitychange', () => {
    if (window.fingerTunnel) {
        if (document.hidden) {
            window.fingerTunnel.stop();
        } else {
            window.fingerTunnel.start();
        }
    }
});