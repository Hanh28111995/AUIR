



/////////////////////////////////////////// scroll page slider /////////////////////////////////////////////

$(document).ready(function () {

    var box = document.querySelectorAll('.box'),
        indx = 0,
        Anim;



    document.addEventListener("mousewheel", Go);
    document.addEventListener("DOMMouseScroll", Go);
    window.addEventListener("load", GoTop);

    function getScaleY_FromMatrix(matrix) {
        let matrixValues = matrix.match(/matrix.*\((.+)\)/)[1].split(', ');
        let scaleY = parseFloat(matrixValues[matrixValues.length - 1]);
        return (scaleY / window.innerHeight) * 100
    }

    function GoTop() {
        for (var i = box.length - 1; i >= 0; i--) {
            box[i].anim = TweenLite.to(box[i], 0.7, { yPercent: i * 100, pause: true });
        }
    }

    function Go(e) {
        var SD = isNaN(e) ? e.wheelDelta || -e.detail : e;
        var activeStatus = $(box[indx]).find(".page").data('action');
        var activePage = $(box[indx]).find(".page").attr('id') + "Animation";

        if (activeStatus === 0) {
            console.log(activePage)
            eval(activePage)(SD, indx);
        }
        else {
            if (SD > 0 && indx > 0) {
                if (!Anim || !Anim.isActive()) {
                    for (var i = 0; i < box.length; i++) {
                        let position = getScaleY_FromMatrix($(box[i]).css('transform'));
                        Anim = TweenLite.to(box[i], 0.7, { yPercent: position + 100 });
                    }
                    indx--;
                }
            }

            else if (SD < 0 && indx < box.length - 1) {
                if (!Anim || !Anim.isActive()) {
                    indx++;
                    for (var i = 0; i < box.length; i++) {
                        let position = getScaleY_FromMatrix($(box[i]).css('transform'));
                        Anim = TweenLite.to(box[i], 0.7, { yPercent: position - 100 });
                    }
                    if (EndAnimateOfPage === true) EndAnimateOfPage = false;
                }
            }
        }
    }


    //$('#up').click(function () {
    //    indx++;
    //    for (var i = 0; i < box.length; i++) {
    //        let position = getScaleY_FromMatrix($(box[i]).css('transform'));
    //        Anim = TweenLite.to(box[i], 0.7, { yPercent: position - 100 });
    //    }
    //})
    //$('#down').click(function () {
    //    for (var i = box.length - 1; i >= 0; i--) {
    //        box[i].anim = TweenLite.to(box[i], 0.7, { yPercent: i * 100, pause: true });
    //    }    
    //    for (var i = 0; i < box.length; i++) {
    //        let position = getScaleY_FromMatrix($(box[i]).css('transform'));
    //        Anim = TweenLite.to(box[i], 0.7, { yPercent: position + 100 });
    //    }
    //    indx--;
    //})
});





/////////////////// click to scroll next page /////////////////////////
//var fix_for_click = 35;

//$(document).ready(function () {
//    $(".section").click(function ()
//    {
//        const dataActionValue = this.getAttribute('data-action');
//        const idValue = this.getAttribute('id');
//        console.log(idValue);
//        if (dataActionValue == 0 ) {
//            if (idValue == 'Home') {
//            HomeAnimation(this);
//                this.setAttribute('data-action', 1);
//            }
//            if (idValue == 'About') {
//                AboutAnimation(this);
//            }
//        }
//        else {
//            var fuller = $(this).closest(".page");
//            var find_child = $(this).find(".page-expand");
//            if (find_child.length) {
//                // found!
//                section = $(this).closest(".section");
//                section.animate({
//                    scrollTop: section.scrollTop() + find_child.height() + fix_for_click
//                }, 700);

//                $(this).find(".page-expand").removeClass("page-expand").addClass("page-expanded");
//            }
//            else {
//                var find_chil = $(this).find(".page-expanded");

//                if (find_chil.length != 0) {
//                    let x = find_chil.offset().top + find_chil.height() + parseInt(find_chil.closest('.page').next().css('margin-top'), 10);

//                    $('.section').animate({
//                        scrollTop: section.scrollTop() + find_chil.offset().top + parseInt(find_chil.closest('.page').next().css('margin-top') || '0', 10)
//                    }, 700);
//                }
//                else {
//                    section = $(this).closest(".section");
//                    section.animate({
//                        scrollTop: section.scrollTop() + fuller.height()
//                    }, 700);
//                }
//            }
//        }
//    });
//});


////////////////event click navlink ////////////////////

//document.addEventListener('DOMContentLoaded', function () {
//    var navLinks = document.querySelectorAll('.nav-link');
//    navLinks.forEach(function (navLink) {
//        navLink.addEventListener('click', function (event) {
//            event.preventDefault();
//            var targetId = this.getAttribute('href').substring(1);

//            var targetElement = document.getElementById(targetId);
//            if (targetElement) {
//                var targetOffsetTop = targetElement.offsetTop - 150;
//                var element = document.getElementById("section");
//                element.scrollTo({
//                    top: targetOffsetTop,
//                    behavior: 'smooth'
//                });

//                var navL = document.querySelector('.nav-item a[href*=' + targetId + ']');
//                if (navL) {
//                    $('.nav-item').removeClass('active');
//                    navL.closest('li').classList.add('active');
//                }
//            }
//        });
//    });
//});



////////////gsap setting //////////////////////////////

//gsap.to(".text-existed", {
//    y: "-100%", delay: 0, duration: 1,
//})
//gsap.to(".text-removed", {
//    y: "0", delay: 0, duration: 1,
//})
//gsap.to(".text-existed-s", {
//    y: "-100%", delay: 0, duration: 1,
//})
//gsap.to(".text-removed-s", {
//    y: "-100%", delay: 0, duration: 1,
//})
let initialAnimationCompleted = false;
let initialAnimationCompleted1 = false;
let initialAnimationCompleted2 = false;
let initialAnimationCompleted3 = false;
let EndAnimateOfPage = false;


const HomeAnimation = (direct, indx) => {
    console.log(EndAnimateOfPage)
    let box = document.querySelectorAll('.box')
    function animateHomePage() {
        let ExpandDis = $(".home-page").next('.page-expand').height();
        tl.to(".home-page", 1, { y: (-1) * ExpandDis, ease: Power1.easeInOut });
        tl.to(".home-page-expand", 1, { y: (-1) * ExpandDis, ease: Power1.easeInOut }, 0);
    }
    function animateHomePageDefault() {
        tl.to(".home-page", 1, { y: 0, ease: Power1.easeInOut });
        tl.to(".home-page-expand", 1, { y: 0, ease: Power1.easeInOut }, 0);
    }

    var tl = new TimelineLite();

    if (EndAnimateOfPage === true) {
        if (direct < 0) {
            $(box[indx]).find('.page').data('action', 1);
            $(box[indx + 1]).find('.page').data('action', 0);
        }
    }

    if (direct < 0) {
        if (!initialAnimationCompleted) {
            tl.to(".text-existed", 1, { y: "-100%", ease: Power1.easeInOut })
                .to(".text-removed", 1, { y: "0%", ease: Power1.easeInOut }, 0)
                .to(".text-existed-s", 1, { y: "-100%", ease: Power1.easeInOut }, 0)
                .to(".text-removed-s", 1, { y: "-100%", ease: Power1.easeInOut }, 0);
            initialAnimationCompleted = true;
        } else {
            animateHomePage();
            EndAnimateOfPage = true;
        }
    }
    if (direct > 0) {
        if (initialAnimationCompleted) {
            animateHomePageDefault();
            initialAnimationCompleted = false;
            EndAnimateOfPage = false;
        }
        else {
            tl.to(".text-existed", 1, { y: "0%", ease: Power1.easeInOut })
                .to(".text-removed", 1, { y: "100%", ease: Power1.easeInOut }, 0)
                .to(".text-existed-s", 1, { y: "0%", ease: Power1.easeInOut }, 0)
                .to(".text-removed-s", 1, { y: "0%", ease: Power1.easeInOut }, 0);
        }

    }

}

const AboutAnimation = (direct, indx) => {
    console.log(EndAnimateOfPage)
    let box = document.querySelectorAll('.box')

    function animateAboutPage() {
        let ExpandDis = $(".about-page").next('.page-expand').height();
        tl.to(".about-page", 1, { y: (-1) * ExpandDis, ease: Power1.easeInOut });
        tl.to(".about-page-expand", 1, { y: (-1) * ExpandDis, ease: Power1.easeInOut }, 0);
    }
    function animateAboutDefault() {
        tl.to(".about-page", 1, { y: 0, ease: Power1.easeInOut });
        tl.to(".about-page-expand", 1, { y: 0, ease: Power1.easeInOut }, 0);
    }

    var tl = new TimelineLite();

    if (EndAnimateOfPage === true) {
        if (direct < 0) {
            $(box[indx]).find('.page').data('action', 1);
            $(box[indx + 1]).find('.page').data('action', 0);
        }
    }


    if (direct < 0) {
        if (!(initialAnimationCompleted1 && initialAnimationCompleted2 && initialAnimationCompleted3)) {
            if (!initialAnimationCompleted1) {
                tl.to(".img-right", 1, { right: "90%", opacity: 1, ease: Power1.easeInOut })
                    .to(".img-left", 1, { left: "80%", opacity: 1, ease: Power1.easeInOut }, 0)
                    .to(".img-ct", 1, { scale: 0.3, ease: Power1.easeInOut }, 0)
                    .to(".bg-img", 1, { scale: 2, ease: Power1.easeInOut }, 0)
                    .call(() => {
                        initialAnimationCompleted1 = true;
                    })
            }

            if (!initialAnimationCompleted2 && initialAnimationCompleted1) {
                tl.to(".img-right", 0.5, { top: "45%", ease: Power1.easeInOut })
                    .to(".img-left", 0.5, { top: "55%", ease: Power1.easeInOut }, 0)
                    .to(".img-ct", 0.5, { y: "-100%", opacity: 0, ease: Power1.easeInOut }, 0)
                    .to(".hor-wrap", 0.5, { bottom: "50%", opacity: 1, ease: Power1.easeInOut }, 0)                    
                    .call(() => {
                        initialAnimationCompleted2 = true;
                    })
            }
            if (!initialAnimationCompleted3 && initialAnimationCompleted2 && initialAnimationCompleted1) {
                tl.to(".hor-wrap p:first-of-type", 1, { x: "-50%", opacity: 0, ease: Power1.easeInOut })
                    .to(".hor-wrap p:last-of-type", 1, { x: "-50%", opacity: 1, ease: Power1.easeInOut }, 0)
                    .call(() => {
                        initialAnimationCompleted3 = true;
                    })
            }
        }
        else {
            console.log('check')
            animateAboutPage();
            EndAnimateOfPage = true;
        }
    } 


    if (direct > 0) {
        if (initialAnimationCompleted1 && initialAnimationCompleted2 && initialAnimationCompleted3) {
            animateAboutDefault();
            initialAnimationCompleted3 = false;
            EndAnimateOfPage = false;
        }
        else {
            tl.to(".img-right", 1, { right: "0", opacity: 0, ease: Power1.easeInOut })
                .to(".img-left", 1, { left: "0", opacity: 0, ease: Power1.easeInOut }, 0)
                .to(".img-ct", 1, { scale: 1, ease: Power1.easeInOut }, 0)
                .to(".bg-img", 1, { scale: 1, ease: Power1.easeInOut }, 0)
        }

    }

}



    //})
    //gsap.to(".img-left", {
    //    left: "80%", delay: 0, duration: 1.5, opacity: 1
    //})
    //gsap.to(".img-ct", {
    //    scale: 0.3, delay: 0, duration: 1.5
    //})
    //gsap.to(".bg-img", {
    //    scale: 2, delay: 0, duration: 1.5,
    //    onComplete: function () {
    //        ele.addEventListener("click", function () {
    //            gsap.to(".img-right", {
    //                top: "45%", delay: 0, duration: 1,
    //            })
    //            gsap.to(".img-left", {
    //                top: "55%", delay: 0, duration: 1,
    //            })
    //            gsap.to(".img-ct", {
    //                y: "-100%", delay: 0, opacity: 0, duration: 1,
    //            })
    //            gsap.to(".hor-wrap", {
    //                bottom: "50%", delay: 0, opacity: 1, duration: 1,
    //                onComplete: function () {
    //                    ele.addEventListener("click", function () {
    //                        gsap.to(".hor-wrap p:first-of-type", {
    //                            x: "-100%", delay: 0, opacity: 0, duration: 1
    //                        })
    //                        gsap.to(".hor-wrap p:last-of-type", {
    //                            x: "0%", delay: 0, opacity: 1, duration: 1
    //                        })
    //                    })
    //                }
    //            })
    //        })
    //    }
    //})
//}


//////////////// slats blind ////////////////////////////
//var slatCount = 10;

//function horizontalBlinds(elementSelector) {
//    var containerHeight = $(elementSelector).height();
//    var slatHeight = containerHeight / slatCount;

//    for (var count = 0; count < slatCount; count++) {
//        var newSlat = $('<div class="slat"></div>');
//        $(elementSelector).children().clone().appendTo(newSlat);
//        $(elementSelector).append(newSlat);
//        newSlat.css("height", slatHeight);
//    }

//    $(elementSelector + ">.paralax").css('display', 'none');
//    $(elementSelector + " .projects-detail-item-content").hide();
//    $(".see-all-projects").hide();

//    var horizalPieces = $(elementSelector + " > .slat");
//    var animationDuration = 100; // Adjust as needed
//    for (let x = horizalPieces.length - 1; x >= 0; x--) {
//        slatBlinds(x);
//    }

//    function slatBlinds(x) {

//        setTimeout(function () {
//            $(horizalPieces[x]).animate({ top: (x + 1) * (-10) + "%" }, (slatCount - (x + 1)) * animationDuration);
//        }, (slatCount - x) * animationDuration)
//    }
//}

//$(document).ready(function () {
//    $('#blindbox').on('click', function () {
//        horizontalBlinds('#blindbox');
//    });
//});




