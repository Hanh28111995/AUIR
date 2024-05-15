
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

/////////////////////////////////////////// scroll page slider /////////////////////////////////////////////
var OneActionOneScroll = false;
$(document).ready(function () {

    var box = document.querySelectorAll('.box'),
        indx = 0,
        Anim;
    var numSliders = document.querySelectorAll('.projects-detail-item'),
        numS = 0;

    const vScrollList = document.querySelector('.js-vertical-scroll-list');

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
    var OneActionOneScroll = true;
    function Go(e) {
        if (OneActionOneScroll) {
            OneActionOneScroll = false;

            var SD = isNaN(e) ? e.wheelDelta || -e.detail : e;
            var activeStatus = $(box[indx]).find(".page").data('action');
            var activePage = $(box[indx]).find(".page").attr('id') + "Animation";
            var x = eval(activePage)(SD, indx);

            if (x === true) {
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

            setTimeout(function () {
                OneActionOneScroll = true;
            }, 200);
        }
    }

    document.addEventListener("mousewheel", Go);


    document.addEventListener("DOMMouseScroll", Go);

    window.addEventListener("load", GoTop);


////////init scroll list ////////
    let depth = 0;
    let item = vScrollList.querySelector('.js-vertical-scroll-list-item');
    let items = vScrollList.querySelectorAll('.js-vertical-scroll-list-item');
    let itemHeight = parseFloat(getComputedStyle(item, null).height.replace("px", ""));
    $(vScrollList).css('height', itemHeight * 3);

    let HomeAnimationCompleted = false;
    let AboutAnimationCompleted1 = false;
    let AboutAnimationCompleted2 = false;
    let AboutAnimationCompleted3 = false;
    let ProAnimationCompleted = false;
    let EndAnimateOfPage = false;
    let ProjectAnimationCompleted1 = false;
    let ProjectAnimationCompleted2 = false;


    const HomeAnimation = (direct, indx) => {
        let box = document.querySelectorAll('.box')
        function animateHomePage() {
            let ExpandDis = $(".home-page").next('.page-expand').height();
            tl.to(".home-page", 0.5, { y: (-1) * ExpandDis, ease: Power1.easeInOut });
            tl.to(".home-page-expand", 0.5, { y: (-1) * ExpandDis, ease: Power1.easeInOut }, 0);
        }
        function animateHomePageDefault() {
            tl.to(".home-page", 0.5, { y: 0, ease: Power1.easeInOut });
            tl.to(".home-page-expand", 0.5, { y: 0, ease: Power1.easeInOut }, 0);
        }

        var tl = new TimelineLite();

        if (EndAnimateOfPage === true) {
            if (direct < 0) {
                return true
            }
            if (direct > 0) {
                EndAnimateOfPage = false;
            }
        }

        if (direct < 0) {
            if (!HomeAnimationCompleted) {
                tl.to(".text-existed", 0.5, { y: "-100%", ease: Power1.easeInOut })
                    .to(".text-removed", 0.5, { y: "0%", ease: Power1.easeInOut }, 0)
                    .to(".text-existed-s", 0.5, { y: "-100%", ease: Power1.easeInOut }, 0)
                    .to(".text-removed-s", 0.5, { y: "-100%", ease: Power1.easeInOut }, 0)
                    .call(() => {
                        HomeAnimationCompleted = true;
                    });
            } else {
                animateHomePage();
                EndAnimateOfPage = true;
            }
        }
        if (direct > 0) {
            if (HomeAnimationCompleted) {
                animateHomePageDefault();
                HomeAnimationCompleted = false;
                return HomeAnimationCompleted
            }
            else {
                tl.to(".text-existed", 0.5, { y: "0%", ease: Power1.easeInOut })
                    .to(".text-removed", 0.5, { y: "100%", ease: Power1.easeInOut }, 0)
                    .to(".text-existed-s", 0.5, { y: "0%", ease: Power1.easeInOut }, 0)
                    .to(".text-removed-s", 0.5, { y: "0%", ease: Power1.easeInOut }, 0);
            }
        }
        return false

    }

    const AboutAnimation = (direct, indx) => {
        let box = document.querySelectorAll('.box')
        var tl = new TimelineLite();
        if (direct < 0 && EndAnimateOfPage === true) {
            return true
        }
        if ((direct > 0) && (!AboutAnimationCompleted2 && !AboutAnimationCompleted1 && !AboutAnimationCompleted3)) {
            return true
        }
        if (direct < 0) {
            if (AboutAnimationCompleted3 && !AboutAnimationCompleted1 && !AboutAnimationCompleted2) {
                tl.to(".hor-wrap p:first-of-type", 0.5, { x: "-50%", opacity: 0, ease: Power1.easeInOut })
                    .to(".hor-wrap p:last-of-type", 0.5, { x: "-50%", opacity: 1, ease: Power1.easeInOut }, 0);
                AboutAnimationCompleted3 = true;
                AboutAnimationCompleted2 = true;
                AboutAnimationCompleted1 = true;
                EndAnimateOfPage = true;
            }

            if (AboutAnimationCompleted2 && !AboutAnimationCompleted1 && !AboutAnimationCompleted3) {
                tl.to(".img-right", 0.5, { top: "45%", ease: Power1.easeInOut })
                    .to(".img-left", 0.5, { top: "55%", ease: Power1.easeInOut }, 0)
                    .to(".img-ct", 0.5, { y: "-100%", opacity: 0, ease: Power1.easeInOut }, 0)
                    .to(".hor-wrap", 0.5, { bottom: "50%", opacity: 1, ease: Power1.easeInOut }, 0);

                AboutAnimationCompleted2 = false;
                AboutAnimationCompleted1 = false;
                AboutAnimationCompleted3 = true;

            }

            if (!AboutAnimationCompleted1 && !AboutAnimationCompleted3 && !AboutAnimationCompleted2) {
                tl.to(".img-right", 0.5, { right: "90%", opacity: 1, ease: Power1.easeInOut })
                    .to(".img-left", 0.5, { left: "80%", opacity: 1, ease: Power1.easeInOut }, 0)
                    .to(".img-ct", 0.5, { scale: 0.3, ease: Power1.easeInOut }, 0)
                    .to(".bg-img", 0.5, { scale: 2, ease: Power1.easeInOut }, 0);

                AboutAnimationCompleted1 = false;
                AboutAnimationCompleted2 = true;
                AboutAnimationCompleted3 = false;
            }
        }

        if (direct > 0) {

            if (!AboutAnimationCompleted2 && AboutAnimationCompleted1 && !AboutAnimationCompleted3) {
                tl.to(".img-right", 0.5, { right: "0%", opacity: 0, ease: Power1.easeInOut })
                    .to(".img-left", 0.5, { left: "0%", opacity: 0, ease: Power1.easeInOut }, 0)
                    .to(".img-ct", 0.5, { scale: 1, y: 0, opacity: 1, ease: Power1.easeInOut }, 0)
                    .to(".hor-wrap", 0.5, { bottom: "0", opacity: 0, ease: Power1.easeInOut }, 0)
                    .to(".bg-img", 0.5, { scale: 1, ease: Power1.easeInOut }, 0);
                AboutAnimationCompleted1 = false;
                AboutAnimationCompleted2 = false;
                AboutAnimationCompleted3 = false;
                EndAnimateOfPage = false;
            }

            if (AboutAnimationCompleted2 && !AboutAnimationCompleted1 && !AboutAnimationCompleted3) {
                tl.to(".img-right", 0.5, { right: "90%", opacity: 1, ease: Power1.easeInOut })
                    .to(".img-left", 0.5, { left: "80%", opacity: 1, ease: Power1.easeInOut }, 0)
                    .to(".img-ct", 0.5, { scale: 0.3, ease: Power1.easeInOut }, 0)
                    .to(".bg-img", 0.5, { scale: 2, ease: Power1.easeInOut }, 0);

                AboutAnimationCompleted1 = true;
                AboutAnimationCompleted2 = false;
                AboutAnimationCompleted3 = false;
            }

            if (AboutAnimationCompleted3 && !AboutAnimationCompleted1 && !AboutAnimationCompleted2) {
                tl.to(".img-right", 0.5, { top: "50%", ease: Power1.easeInOut })
                    .to(".img-left", 0.5, { top: "50%", ease: Power1.easeInOut }, 0)
                    .to(".img-ct", 0.5, { y: "0%", opacity: 1, ease: Power1.easeInOut }, 0)
                    .to(".hor-wrap", 0.5, { bottom: "-50%", opacity: 1, ease: Power1.easeInOut }, 0);
                AboutAnimationCompleted1 = false;
                AboutAnimationCompleted2 = true;
                AboutAnimationCompleted3 = false;
            }

            if (AboutAnimationCompleted3 && AboutAnimationCompleted2 && AboutAnimationCompleted1) {
                if (EndAnimateOfPage === false) {
                    tl.to(".hor-wrap p:first-of-type", 0.5, { x: "50%", opacity: 1, ease: Power1.easeInOut })
                        .to(".hor-wrap p:last-of-type", 0.5, { x: "50%", opacity: 0, ease: Power1.easeInOut }, 0);

                    AboutAnimationCompleted2 = false;
                    AboutAnimationCompleted1 = false;
                    AboutAnimationCompleted3 = true;

                }
                else {
                    EndAnimateOfPage = false;
                }
            }

        }

        return false
    }

    const ProAnimation = (direct, indx) => {
        let box = document.querySelectorAll('.box')
        let ExpandDis = $(".pr-title-page").find('.page-expand').height();
        var tl = new TimelineLite();

        if (direct < 0 && EndAnimateOfPage === true) {
            return true
        }

        if (direct > 0 && !ProAnimationCompleted) {
            return true
        }

        if (direct < 0) {
            if (!ProAnimationCompleted) {
                tl.to(".projects-title", 0.5, { scale: 1, ease: Power1.easeInOut })
                    .to(".about-page-expand", 0.5, { y: (-1) * ExpandDis, ease: Power1.easeInOut }, 0)
                    .to(".projects-title", 0.5, { y: (-1) * ExpandDis, ease: Power1.easeInOut }, 0)
                    .call(() => {
                        ProAnimationCompleted = true;
                    })
            } else {
                EndAnimateOfPage = true;
            }
        }

        if (direct > 0) {
            if (ProAnimationCompleted) {
                tl.to(".projects-title", 0.5, { scale: 1.3, ease: Power1.easeInOut })
                    .to(".about-page-expand", 0.5, { y: 0, ease: Power1.easeInOut }, 0)
                    .to(".projects-title", 0.5, { y: 0, ease: Power1.easeInOut }, 0)
                    .call(() => {
                        ProAnimationCompleted = false;
                    });
            }
            else {
                EndAnimateOfPage = false;
            }
        }
        return false
    }
    
    const ProjectAnimation = (direct, indx) => {
        const throttledUp = function () {
            for (var i = 0; i < numSliders.length; i++) {
                let position = getScaleY_FromMatrix($(numSliders[i]).css('transform'));
                tl.to(numSliders[i], 0.5, { yPercent: position - 100 }, 0);
            }

        };
        const throttledDown = function () {
            for (var i = 0; i < numSliders.length; i++) {
                let position = getScaleY_FromMatrix($(numSliders[i]).css('transform'));
                tl.to(numSliders[i], 0.5, { yPercent: position + 100 }, 0);
            }
        };
        var tl = new TimelineLite();
        if (direct < 0 && EndAnimateOfPage === true) {
            return true
        }
        if (direct > 0 && EndAnimateOfPage === false && numS === 0) {
            return true
        }

        if (direct < 0 && ProjectAnimationCompleted2 === true) {
            EndAnimateOfPage = true;
        }
        //if (direct > 0 && numS >= numSliders.length - 1) {
        //    EndAnimateOfPage = false;
        //}

        if (direct < 0) {
            if (numS < numSliders.length - 1 && !ProjectAnimationCompleted2) {
                throttledUp();
                numS++;
            }
            if (ProjectAnimationCompleted1) {
                if (ProjectAnimationCompleted2) {
                    tl.to(".see-all-projects", 0.7, { opacity: 0, ease: Power1.easeInOut }, 0);
                    horizontalBlinds("#last-item");
                }
                tl.to(".see-all-projects", 0.7, { bottom: '50px', ease: Power1.easeInOut });
                ProjectAnimationCompleted2 = true;
            }
            if (numS === numSliders.length - 1) {
                ProjectAnimationCompleted1 = true;
            }
        }

        if (direct > 0) {
            if (numS > 0 && !ProjectAnimationCompleted1) {
                numS--;
                throttledDown();
            }
            if (ProjectAnimationCompleted1) {
                tl.to(".see-all-projects", 0.5, { bottom: '-100%', ease: Power1.easeInOut });
            }
            if (ProjectAnimationCompleted2) {
                horizontalBlindsReverse("#last-item")
            }
            if (ProjectAnimationCompleted1 && numS === numSliders.length - 1 && ProjectAnimationCompleted2 === false) {
                ProjectAnimationCompleted1 = false;
            }
            if (ProjectAnimationCompleted2 === true) {
                ProjectAnimationCompleted2 = false;
            }
        }
        return false
    }
    

    const PjListAnimation = (direct, indx) => {
        var tl = new TimelineLite();
        if (vScrollList !== null) {

            $(vScrollList)
                .on('ux.vScrollList.init', function () {                                        
                    $(this).trigger('ux.vScrollList.setHeight');
                    $(this).trigger('ux.vScrollList.onWheel');
                })
                .on('ux.vScrollList.setHeight', function () {                    
                    let itemHeight = parseFloat(getComputedStyle(item, null).height.replace("px", ""));

                    $(this).css('height', itemHeight * 3);
                })

            let list = vScrollList.querySelector('ul');                        
            if (direct < 0 && depth < items.length -3) {
                depth += 1;
                let value = -itemHeight * depth;
                tl.to(list, 0.7, { y: value, ease: Power1.easeOut });
            }

            if (direct > 0 && depth > 0) {                                
                depth -= 1;
                let value = -itemHeight * depth;
                tl.to(list, 0.7, { y: value, ease: Power1.easeOut });
            }                    
        }

        return false
    }
});

//////////////// slats blind ////////////////////////////
var slatCount = 10;
let completedIterations = 0;
var animationDuration = 100;

function horizontalBlinds(elementSelector) {
    var slatHeight = $(window).height() * (slatCount / 100);
    for (var count = 0; count < slatCount; count++) {
        var newSlat = $('<div class="slat quickflip"></div>');
        $(elementSelector).children().clone().appendTo(newSlat);
        $(elementSelector).append(newSlat);
        newSlat.css("height", slatHeight);
    }
    $(elementSelector + ">.paralax").css('display', 'none');
    $(elementSelector + " .projects-detail-item-content").hide();
    $(".see-all-projects").hide();

    var horizalPieces = $(elementSelector + " > .slat");
    for (let x = horizalPieces.length - 1; x >= 0; x--) {
        slatBlinds(x);
    }

    function slatBlinds(x) {
        $(horizalPieces[x]).find(".paralax").css('background-position', `center ${(x * (-1) * slatHeight)}px`)
        setTimeout(function () {
            var tl = new TimelineLite();
            gsap.set(".quickflip", {
                transformStyle: "preserve-3d",
                transformPerspective: 1000
            });
            $(horizalPieces[x]).css("overflow", "hidden");
            tl.to(horizalPieces[x], { rotationX: 90, duration: 0.5 });
        }, (slatCount - x) * animationDuration)
    }
}

function horizontalBlindsReverse(elementSelector) {
    var horizalPieces = $(elementSelector + " > .slat");
    if (horizalPieces) {
        for (let x = 0; x < horizalPieces.length; x++) {
            slatBlindsReverse(x);
        }
        setTimeout(function () {
            $(elementSelector + ">.paralax").css('display', 'block');
            $(elementSelector + " .projects-detail-item-content").show();
            $(".see-all-projects").show();
            horizalPieces.remove();
        }, slatCount * (animationDuration + 70));
    }
    function slatBlindsReverse(x) {
        setTimeout(function () {
            var tl = new TimelineLite();
            gsap.set(".quickflip", {
                transformStyle: "preserve-3d",
                transformPerspective: 1000
            });
            tl.to(horizalPieces[x], { rotationX: 0, duration: 0.5 });
            completedIterations++;
        }, x * animationDuration)
    }
}









