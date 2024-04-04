var fix_for_click = 35;

$(document).ready(function () {
    $(".page").click(function () {
        var fuller = $(this).closest(".page");
        var find_child = $(this).find(".page-expand");
        if (find_child.length) {
            // found!
            section = $(this).closest(".section");            
            section.animate({
                scrollTop: section.scrollTop() + find_child.height() + fix_for_click
            }, 700);

            $(this).find(".page-expand").removeClass("page-expand").addClass("page-expanded");
        }
        else {
            var find_chil = $(this).find(".page-expanded");
            
            if (find_chil.length != 0) {
                let x = find_chil.offset().top + find_chil.height() + parseInt(find_chil.closest('.page').next().css('margin-top'), 10);

                $('.section').animate({
                    scrollTop: section.scrollTop() + find_chil.offset().top + parseInt(find_chil.closest('.page').next().css('margin-top') || '0', 10)
                }, 700);
            }
            else {
                section = $(this).closest(".section");
                section.animate({
                    scrollTop: section.scrollTop() + fuller.height()
                }, 700);
            }
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



var slatCount = 10;

//function horizontalBlinds(elementSelector) {
//    var currentTop = 0; // Start from the top of the container
//    var count;
//    var containerHeight = $(elementSelector).height();

//    // Determine height of each blind slat
//    var slatHeight = Math.floor(containerHeight / slatCount);
//    var slatLeftOverHeight = containerHeight - slatHeight * slatCount;

//    // Animate each slat to 'shut' the blind
//    for (var count = 0; count < slatCount; count++) {
//        var slat = $(elementSelector + ' .slat').eq(count); // Select existing slats
//        var actualHeight;
//        if (count == slatCount - 1)
//            actualHeight = slatHeight + slatLeftOverHeight;
//        else
//            actualHeight = slatHeight;

//        // Delay each animation by count times 500 milliseconds
//        slat.delay((slatCount - count - 1) * 500).animate({ height: actualHeight + 'px', top: (containerHeight - actualHeight) + 'px' });
//    }
//}


function horizontalBlinds(elementSelector) {
    var containerHeight = $(elementSelector).height();
    var slatHeight = containerHeight / slatCount;

    // Animate each slat to reveal the absolute element below it
    for (var count = 0; count < slatCount; count++) {
        var slat = $(elementSelector + '#blindbox');
        slat.animate({ height: slatHeight * (count + 1) }, 500);
    }
}

$(document).ready(function () {
    $('.see-all-projects').on('click', function () {
        horizontalBlinds('#blindbox');
    });
});


