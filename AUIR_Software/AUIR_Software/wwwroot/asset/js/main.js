



/////////////////////////////////////////// scroll page slider /////////////////////////////////////////////
var OneActionOneScroll = false;
$(document).ready(function () {

    var box = document.querySelectorAll('.box'),
        indx = 0,
        Anim;
    var numSliders = document.querySelectorAll('.projects-detail-item'),
        numS = 0;

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
        OneActionOneScroll = true;
        setTimeout(function () {
            OneActionOneScroll = false;
        }, 200); // Adjust the delay as needed
    }

    document.addEventListener("mousewheel", function (e) {
        if (!OneActionOneScroll) {
            Go(e);
        }
    });
    document.addEventListener("DOMMouseScroll", function (e) {
        if (!OneActionOneScroll) {
            Go(e);
        }
    });
    window.addEventListener("load", GoTop);

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



    let initialAnimationCompleted = false;
    let initialAnimationCompleted1 = false;
    let initialAnimationCompleted2 = false;
    let initialAnimationCompleted3 = false;
    let initialAnimationCompleted4 = false;
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
        let box = document.querySelectorAll('.box')

        function animateAboutPage() {
        }
        function animateAboutDefault() {
        }

        var tl = new TimelineLite();

        if (EndAnimateOfPage === true) {
            if (direct < 0 && (initialAnimationCompleted3 && initialAnimationCompleted2 && initialAnimationCompleted1)) {
                $(box[indx]).find('.page').data('action', 1);
                $(box[indx + 1]).find('.page').data('action', 0);
            }
        }
        if (EndAnimateOfPage === false) {
            if (direct > 0 && (!initialAnimationCompleted3 && !initialAnimationCompleted2 && !initialAnimationCompleted1)) {
                $(box[indx - 1]).find('.page').data('action', 0);
                $(box[indx]).find('.page').data('action', 1);
            }
        }


        if (direct < 0) {

            if (initialAnimationCompleted3 && initialAnimationCompleted2 && initialAnimationCompleted1) {
                animateAboutPage();
                EndAnimateOfPage = true;
            }

            if (initialAnimationCompleted3 && !initialAnimationCompleted1 && !initialAnimationCompleted2) {
                tl.to(".hor-wrap p:first-of-type", 1, { x: "-50%", opacity: 0, ease: Power1.easeInOut })
                    .to(".hor-wrap p:last-of-type", 1, { x: "-50%", opacity: 1, ease: Power1.easeInOut }, 0);
                initialAnimationCompleted3 = true;
                initialAnimationCompleted2 = true;
                initialAnimationCompleted1 = true;
            }

            if (initialAnimationCompleted2 && !initialAnimationCompleted1 && !initialAnimationCompleted3) {
                tl.to(".img-right", 0.5, { top: "45%", ease: Power1.easeInOut })
                    .to(".img-left", 0.5, { top: "55%", ease: Power1.easeInOut }, 0)
                    .to(".img-ct", 0.5, { y: "-100%", opacity: 0, ease: Power1.easeInOut }, 0)
                    .to(".hor-wrap", 0.5, { bottom: "50%", opacity: 1, ease: Power1.easeInOut }, 0);
                initialAnimationCompleted2 = false;
                initialAnimationCompleted1 = false;
                initialAnimationCompleted3 = true;
            }

            if (!initialAnimationCompleted1 && !initialAnimationCompleted3 && !initialAnimationCompleted2) {
                console.log('v-1 :' + initialAnimationCompleted1, 'v-2 :' + initialAnimationCompleted2, 'v-3 :' + initialAnimationCompleted3)
                tl.to(".img-right", 1, { right: "90%", opacity: 1, ease: Power1.easeInOut })
                    .to(".img-left", 1, { left: "80%", opacity: 1, ease: Power1.easeInOut }, 0)
                    .to(".img-ct", 1, { scale: 0.3, ease: Power1.easeInOut }, 0)
                    .to(".bg-img", 1, { scale: 2, ease: Power1.easeInOut }, 0);
                initialAnimationCompleted1 = false;
                initialAnimationCompleted2 = true;
                initialAnimationCompleted3 = false;
            }

        }


        if (direct > 0) {
            if (!initialAnimationCompleted2 && initialAnimationCompleted1 && !initialAnimationCompleted3) {
                tl.to(".img-right", 1, { right: "0%", opacity: 0, ease: Power1.easeInOut })
                    .to(".img-left", 1, { left: "0%", opacity: 0, ease: Power1.easeInOut }, 0)
                    .to(".img-ct", 1, { scale: 1, y: 0, opacity: 1, ease: Power1.easeInOut }, 0)
                    .to(".hor-wrap", 1, { bottom: "0", opacity: 0, ease: Power1.easeInOut }, 0)
                    .to(".bg-img", 1, { scale: 1, ease: Power1.easeInOut }, 0);
                initialAnimationCompleted1 = false;
                initialAnimationCompleted2 = false;
                initialAnimationCompleted3 = false;
            }

            if (initialAnimationCompleted2 && !initialAnimationCompleted1 && !initialAnimationCompleted3) {
                tl.to(".img-right", 1, { right: "90%", opacity: 1, ease: Power1.easeInOut })
                    .to(".img-left", 1, { left: "80%", opacity: 1, ease: Power1.easeInOut }, 0)
                    .to(".img-ct", 1, { scale: 0.3, ease: Power1.easeInOut }, 0)
                    .to(".bg-img", 1, { scale: 2, ease: Power1.easeInOut }, 0);
                initialAnimationCompleted1 = true;
                initialAnimationCompleted2 = false;
                initialAnimationCompleted3 = false;
            }

            if (initialAnimationCompleted3 && !initialAnimationCompleted1 && !initialAnimationCompleted2) {
                tl.to(".img-right", 0.5, { top: "50%", ease: Power1.easeInOut })
                    .to(".img-left", 0.5, { top: "50%", ease: Power1.easeInOut }, 0)
                    .to(".img-ct", 0.5, { y: "0%", opacity: 1, ease: Power1.easeInOut }, 0)
                    .to(".hor-wrap", 0.5, { bottom: "-50%", opacity: 1, ease: Power1.easeInOut }, 0);
                initialAnimationCompleted1 = false;
                initialAnimationCompleted2 = true;
                initialAnimationCompleted3 = false;
            }

            if (initialAnimationCompleted3 && initialAnimationCompleted2 && initialAnimationCompleted1) {
                if (EndAnimateOfPage === false) {
                    tl.to(".hor-wrap p:first-of-type", 1, { x: "50%", opacity: 1, ease: Power1.easeInOut })
                        .to(".hor-wrap p:last-of-type", 1, { x: "50%", opacity: 0, ease: Power1.easeInOut }, 0)
                        .call(() => {
                            initialAnimationCompleted2 = false;
                            initialAnimationCompleted1 = false;
                            initialAnimationCompleted3 = true;
                        })
                }
                else {
                    animateAboutDefault();
                    EndAnimateOfPage = false;
                }
            }

        }
        //}

    }

    const ProAnimation = (direct, indx) => {
        let box = document.querySelectorAll('.box')
        let ExpandDis = $(".pr-title-page").find('.page-expand').height();
        var tl = new TimelineLite();

        if (EndAnimateOfPage === true) {
            if (direct < 0 && initialAnimationCompleted4) {
                $(box[indx]).find('.page').data('action', 1);
                $(box[indx + 1]).find('.page').data('action', 0);
            }
        }
        if (EndAnimateOfPage === false) {
            if (direct > 0 && !initialAnimationCompleted4) {
                $(box[indx - 1]).find('.page').data('action', 0);
                $(box[indx]).find('.page').data('action', 1);
            }
        }

        if (direct < 0) {
            if (!initialAnimationCompleted4) {
                tl.to(".projects-title", 1, { scale: 1, ease: Power1.easeInOut })
                    .to(".about-page-expand", 1, { y: (-1) * ExpandDis, ease: Power1.easeInOut }, 0)
                    .to(".projects-title", 1, { y: (-1) * ExpandDis, ease: Power1.easeInOut }, 0);
                initialAnimationCompleted4 = true;
            } else {
                EndAnimateOfPage = true;
            }
        }
        if (direct > 0) {
            if (initialAnimationCompleted4) {
                initialAnimationCompleted4 = false;
                tl.to(".projects-title", 1, { scale: 1.3, ease: Power1.easeInOut })
                    .to(".about-page-expand", 1, { y: 0, ease: Power1.easeInOut }, 0)
                    .to(".projects-title", 1, { y: 0, ease: Power1.easeInOut }, 0);
            }
            else {
                EndAnimateOfPage = false;
            }
        }
    }

    const ProjectAnimation = (direct, indx) => {
        const throttledUp = function () {
            for (var i = 0; i < numSliders.length; i++) {
                let position = getScaleY_FromMatrix($(numSliders[i]).css('transform'));
                tl.to(numSliders[i], 0.7, { yPercent: position - 100 }, 0);
            }
        };
        const throttledDown = function () {
            for (var i = 0; i < numSliders.length; i++) {
                let position = getScaleY_FromMatrix($(numSliders[i]).css('transform'));
                tl.to(numSliders[i], 0.7, { yPercent: position + 100 }, 0);
            }
        };

        var tl = new TimelineLite();

        if (numS >= 0) {
            EndAnimateOfPage = false;
        }
        if (numS >= numSliders.length - 1) {
            EndAnimateOfPage = true;
        }

        if (EndAnimateOfPage === true && numS === numSliders.length - 1) {
            if (direct < 0) {
                $(box[indx]).find('.page').data('action', 1);
                $(box[indx + 1]).find('.page').data('action', 0);
            }
        }
        if (EndAnimateOfPage === false && numS === 0) {
            if (direct > 0) {
                $(box[indx - 1]).find('.page').data('action', 0);
                $(box[indx]).find('.page').data('action', 1);
            }
        }


        if (direct < 0) {
            if (numS < numSliders.length - 1) {
                throttledUp();
                numS++;
            }

        }

        if (direct > 0) {
            if (numS > 0) {
                numS--;
                throttledDown();
            }
        }
    }
});

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




