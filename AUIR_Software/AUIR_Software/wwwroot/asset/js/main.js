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



////////////gsap setting //////////////////////////////
gsap.registerPlugin(ScrollTrigger);

document.getElementById("Home").addEventListener("click", function () {
    console.log('ready')
gsap.to(".text-existed", {
    y: "-100%", delay: 0, duration: 1,         
})   
gsap.to(".text-removed", {
    y: "0", delay: 0, duration: 1,
})   
gsap.to(".text-existed-s", {
    y: "-100%", delay: 0, duration: 1,
})
    gsap.to(".text-removed-s", {
        y: "-100%", delay: 0, duration: 1,
        onComplete: function () {
            document.getElementById("Home").addEventListener("click", function () {
                console.log("check")
                gsap.to(".home-page-expand",
                    {
                        y: "100%", 
                        duration: 1, 
                        scrollTrigger:
                        {
                            trigger: "#Home", // Element that triggers the animation
                            start: "top 100%", // Animation starts when the trigger element is 80% from the top of the viewport
                            end: "top 50%", // Animation ends when the trigger element is 20% from the bottom of the viewport
                            scrub: true, // Smoothly animates the elements based on the scroll
                            markers: true, // For demonstration purpose: shows markers indicating the start and end of the scroll trigger
                        }
                    })
            })
        }
    })
})



    