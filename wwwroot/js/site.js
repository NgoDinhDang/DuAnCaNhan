// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Particle effect for banner
document.addEventListener('DOMContentLoaded', () => {
    // Hiệu ứng Typing cho tiêu đề
    const title = document.querySelector('.hero-banner h1');
    if (title) {
        const text = title.textContent;
        title.textContent = '';
        let i = 0;

        function typeWriter() {
            if (i < text.length) {
                title.textContent += text.charAt(i);
                i++;
                setTimeout(typeWriter, 100);
            }
        }
        setTimeout(typeWriter, 500); // Bắt đầu sau 0.5s để đồng bộ với slideIn
    }

    // Hiệu ứng Particle cải tiến
    const heroBanner = document.querySelector('.hero-banner');
    if (heroBanner) {
        const canvas = document.createElement('canvas');
        canvas.className = 'particles';
        const ctx = canvas.getContext('2d');
        heroBanner.appendChild(canvas);

        let particlesArray = [];
        const numberOfParticles = window.innerWidth < 768 ? 50 : 80; // Giảm số hạt trên di động
        let mouse = { x: null, y: null };

        canvas.width = window.innerWidth;
        canvas.height = heroBanner.clientHeight;

        window.addEventListener('resize', () => {
            canvas.width = window.innerWidth;
            canvas.height = heroBanner.clientHeight;
        });

        // Theo dõi vị trí chuột
        window.addEventListener('mousemove', (event) => {
            const rect = canvas.getBoundingClientRect();
            mouse.x = event.clientX - rect.left;
            mouse.y = event.clientY - rect.top;
        });

        class Particle {
            constructor() {
                this.x = Math.random() * canvas.width;
                this.y = Math.random() * canvas.height;
                this.size = Math.random() * 4 + 1;
                this.speedX = Math.random() * 1 - 0.5;
                this.speedY = Math.random() * 1 - 0.5;
                this.color = `rgba(255, 215, 0, ${Math.random() * 0.5 + 0.3})`; // Màu vàng nhạt
            }

            update() {
                // Tương tác với chuột
                if (mouse.x && mouse.y) {
                    const dx = mouse.x - this.x;
                    const dy = mouse.y - this.y;
                    const distance = Math.sqrt(dx * dx + dy * dy);
                    const maxDistance = 100;
                    if (distance < maxDistance) {
                        this.speedX -= dx / 2000;
                        this.speedY -= dy / 2000;
                    }
                }

                this.x += this.speedX;
                this.y += this.speedY;

                if (this.x > canvas.width || this.x < 0) this.speedX *= -1;
                if (this.y > canvas.height || this.y < 0) this.speedY *= -1;
            }

            draw() {
                ctx.fillStyle = this.color;
                ctx.beginPath();
                ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
                ctx.fill();
                ctx.shadowBlur = 10;
                ctx.shadowColor = '#ffd700';
            }
        }

        function init() {
            particlesArray = [];
            for (let i = 0; i < numberOfParticles; i++) {
                particlesArray.push(new Particle());
            }
        }

        function animate() {
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            for (let i = 0; i < particlesArray.length; i++) {
                particlesArray[i].update();
                particlesArray[i].draw();
            }
            requestAnimationFrame(animate);
        }

        init();
        animate();
    }

    // Hiệu ứng cho các icon trong social-icons
    const socialIcons = document.querySelectorAll('.social-icons a');
    socialIcons.forEach(icon => {
        icon.addEventListener('mouseenter', () => {
            icon.style.transition = 'transform 0.5s ease';
            icon.style.transform = 'rotate(360deg) scale(1.2)';
        });
        icon.addEventListener('mouseleave', () => {
            icon.style.transform = 'rotate(0) scale(1)';
        });
    });

    // Hiệu ứng cho nút hero
    const btnHero = document.querySelector('.btn-hero');
    if (btnHero) {
        btnHero.addEventListener('mouseenter', () => {
            btnHero.style.transition = 'transform 0.4s ease';
            btnHero.style.transform = 'translateY(-5px) scale(1.05)';
        });
        btnHero.addEventListener('mouseleave', () => {
            btnHero.style.transform = 'translateY(0) scale(1)';
        });
    }

    // Hiệu ứng cho nút "Chi tiết" trong card
    const detailButtons = document.querySelectorAll('.btn-sm');
    detailButtons.forEach(btn => {
        btn.addEventListener('mouseenter', () => {
            btn.style.transition = 'transform 0.3s ease';
            btn.style.transform = 'scale(1.1)';
        });
        btn.addEventListener('mouseleave', () => {
            btn.style.transform = 'scale(1)';
        });
    });
});