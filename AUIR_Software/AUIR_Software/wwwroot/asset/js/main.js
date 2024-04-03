$(document).ready(function () {
    $(".page").click(function () {
        var fuller = $(this).closest(".page").next();
        var find_child = $(this).find(".page-expand");
        if (find_child.length) {
            // found!
            section = $(this).closest(".section");
            section.animate({
                scrollTop: section.scrollTop() + find_child.height()
            }, 700);
            $(this).find(".page-expand").removeClass("page-expand").addClass("page-expanded");            
        }
        else {
            var find_child = $(this).find(".page-expanded");                                    
            $('.section').animate({
                scrollTop: find_child.offset().top + find_child.height() +  parseInt(find_child.closest('.page').next().css('margin-top'),10)
            }, 700);
        }



        //if ($(this).closest('.page').height() > fuller.height()) {
        //    console.log($(this).closest('.page').offset().top)
        //    section = $(this).closest('.section');
        //    section.animate({
        //        scrollTop: section.scrollTop() + $(this).closest('.page').height() - fuller.height()
        //    }, 700);
        //    fuller.addClass("page-expand").removeClass("page");
        //}

    });
});


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
//gsap.registerPlugin(ScrollTrigger);

//document.getElementById("Home").addEventListener("click", function () {
//    gsap.to(".text-existed", {
//        y: "-100%", delay: 0, duration: 1,
//    })
//    gsap.to(".text-removed", {
//        y: "0", delay: 0, duration: 1,
//    })
//    gsap.to(".text-existed-s", {
//        y: "-100%", delay: 0, duration: 1,
//    })
//    gsap.to(".text-removed-s", {
//        y: "-100%", delay: 0, duration: 1,
//        onComplete: function () {
//            document.getElementById("Home").addEventListener("click", function () {
//                gsap.to(".home-page-expand",
//                    {
//                        y: "100%",
//                        duration: 1,
//                        scrollTrigger:
//                        {
//                            trigger: "#Home",
//                            start: "top center",
//                            end: "bottom center",
//                            scrub: true,
//                        }
//                    })
//            })
//        }
//    })
//})





