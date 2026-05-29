/**
 * IT Guide Modern Interactive Layer
 */

document.addEventListener('DOMContentLoaded', () => {
    // 1. Reveal on Scroll Logic
    const revealElements = () => {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('active');
                }
            });
        }, { threshold: 0.1 });

        document.querySelectorAll('.reveal').forEach(el => observer.observe(el));
    };

    // 2. Navbar Dynamic Styling
    const handleNavbar = () => {
        const navbar = document.querySelector('.navbar');
        if (!navbar) return;

        window.addEventListener('scroll', () => {
            if (window.scrollY > 50) {
                navbar.style.top = '0';
                navbar.style.margin = '0';
                navbar.style.width = '100%';
                navbar.style.borderRadius = '0';
                navbar.style.padding = '1rem 0';
            } else {
                navbar.style.top = '15px';
                navbar.style.margin = '0 1.5rem';
                navbar.style.width = 'calc(100% - 3rem)';
                navbar.style.borderRadius = '20px';
                navbar.style.padding = '1.25rem 0';
            }
        });
    };

    // 3. Smooth Particle Background (Refined)
    const initParticles = () => {
        const canvas = document.getElementById('bg-canvas');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        let particles = [];
        let width, height;

        const mouse = { x: null, y: null, radius: 180 };
        window.addEventListener('mousemove', (e) => { mouse.x = e.x; mouse.y = e.y; });

        class Particle {
            constructor() {
                this.reset();
            }
            reset() {
                this.x = Math.random() * width;
                this.y = Math.random() * height;
                this.size = Math.random() * 2 + 0.5;
                this.baseX = this.x;
                this.baseY = this.y;
                this.density = (Math.random() * 30) + 5;
                this.color = `rgba(147, 197, 253, ${Math.random() * 0.3 + 0.1})`;
            }
            draw() {
                ctx.fillStyle = this.color;
                ctx.beginPath();
                ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
                ctx.fill();
            }
            update() {
                let dx = mouse.x - this.x;
                let dy = mouse.y - this.y;
                let distance = Math.sqrt(dx * dx + dy * dy);
                if (distance < mouse.radius) {
                    let force = (mouse.radius - distance) / mouse.radius;
                    this.x -= (dx / distance) * force * this.density;
                    this.y -= (dy / distance) * force * this.density;
                } else {
                    if (this.x !== this.baseX) this.x -= (this.x - this.baseX) / 15;
                    if (this.y !== this.baseY) this.y -= (this.y - this.baseY) / 15;
                }
            }
        }

        const init = () => {
            width = window.innerWidth;
            height = window.innerHeight;
            canvas.width = width;
            canvas.height = height;
            particles = [];
            const count = (width * height) / 18000;
            for (let i = 0; i < count; i++) particles.push(new Particle());
        };

        const animate = () => {
            ctx.clearRect(0, 0, width, height);
            particles.forEach(p => { p.update(); p.draw(); });
            connect();
            requestAnimationFrame(animate);
        };

        const connect = () => {
            for (let a = 0; a < particles.length; a++) {
                for (let b = a; b < particles.length; b++) {
                    let dx = particles[a].x - particles[b].x;
                    let dy = particles[a].y - particles[b].y;
                    let d = Math.sqrt(dx * dx + dy * dy);
                    if (d < 110) {
                        ctx.strokeStyle = `rgba(147, 197, 253, ${(1 - d/110) * 0.15})`;
                        ctx.lineWidth = 1;
                        ctx.beginPath();
                        ctx.moveTo(particles[a].x, particles[a].y);
                        ctx.lineTo(particles[b].x, particles[b].y);
                        ctx.stroke();
                    }
                }
            }
        };

        window.addEventListener('resize', init);
        init();
        animate();
    };

    // 4. Hover Card Effect
    const initCardEffects = () => {
        document.querySelectorAll('.hub-card').forEach(card => {
            card.addEventListener('mousemove', (e) => {
                const rect = card.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;
                card.style.setProperty('--mouse-x', `${x}px`);
                card.style.setProperty('--mouse-y', `${y}px`);
            });
        });
    };

    revealElements();
    handleNavbar();
    initParticles();
    initCardEffects();
});
