////////////////event click navlink ////////////////////

document.addEventListener('DOMContentLoaded', function () {
    var navLinks = document.querySelectorAll('.nav-link');

    navLinks.forEach(function (navLink) {

        navLink.addEventListener('click', function (event) {
            event.preventDefault();
            var targetId = this.getAttribute('href').substring(1);
            var targetElement = document.getElementById(targetId);

            if (targetElement) {
                var targetOffsetTop = targetElement.offsetTop - 150;

                window.scrollTo({
                    top: targetOffsetTop,
                    behavior: 'smooth'
                });
                var navL = document.querySelector('.nav-item a[href*=' + targetId + ']');
                if (navL) {
                    navL.closest('li').classList.add('active');
                }
            }
        });
    });
});
window.addEventListener('scroll', scrollActive)
const sections = document.querySelectorAll('section[id]')
function scrollActive() {
    const scrollY = window.pageYOffset
    sections.forEach((current) => {
        const sectionHeight = current.offsetHeight
        const sectionTop = current.offsetTop - 250
        const sectionId = current.getAttribute('id')

        if (scrollY > sectionTop && scrollY <= sectionTop + sectionHeight) {
            var navL = document.querySelector('.nav-item a[href*=' + sectionId + ']');
            if (navL) {
                navL.closest('li').classList.add('active');
            }
        } else {
            var navL = document.querySelector('.nav-item a[href*=' + sectionId + ']');
            if (navL) {
                navL.closest('li').classList.remove('active');
            }
        }
    })
}





