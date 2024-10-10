
document.addEventListener('DOMContentLoaded', function () {


    /////////////////////////////////////////// scroll page slider /////////////////////////////////////////////
    let HomeAnimationCompleted = false;
    let AboutAnimationCompleted1 = false;
    let AboutAnimationCompleted2 = false;
    let AboutAnimationCompleted3 = false;
    let ProAnimationCompleted = false;
    let ProjectAnimationCompleted1 = false;
    let ProjectAnimationCompleted2 = false;
    let ServicesAnimationCompleted = false;
    let ContactAnimationCompleted = false;
    let EndAnimateOfPage = false;
    var OneActionOneScroll = false;

    $(document).ready(function () {
        var box = document.querySelectorAll('.box'),
            indx = 0,
            Anim;
        var numSliders = document.querySelectorAll('.projects-detail-item'),
            numS = 0;

        const vScrollList = document.querySelector('.vertical-scroll-list-page .js-vertical-scroll-list');
        const bScrollList = document.querySelector('.vertical-scroll-list-more-page .js-vertical-scroll-list');

        function getScaleY_FromMatrix(matrix) {
            let matrixValues = matrix.match(/matrix.*\((.+)\)/)[1].split(', ');
            let scaleY = parseFloat(matrixValues[matrixValues.length - 1]);
            return (scaleY / window.innerHeight) * 100
        }

        ////////////////  slick style css  //////////////////////////////

        $('.customer-slider').slick({
            dots: true,
            infinite: true,
            speed: 500,
            fade: true,
            cssEase: 'linear',
        });
        $('.slick-prev').html(`         
       <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-arrow-left-short" viewBox="0 0 16 16">
  <path fill-rule="evenodd" d="M12 8a.5.5 0 0 1-.5.5H5.707l2.147 2.146a.5.5 0 0 1-.708.708l-3-3a.5.5 0 0 1 0-.708l3-3a.5.5 0 1 1 .708.708L5.707 7.5H11.5a.5.5 0 0 1 .5.5"/>
</svg>
        `);
        $('.slick-next').html(`
    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-arrow-right-short" viewBox="0 0 16 16">
  <path fill-rule="evenodd" d="M4 8a.5.5 0 0 1 .5-.5h5.793L8.146 5.354a.5.5 0 1 1 .708-.708l3 3a.5.5 0 0 1 0 .708l-3 3a.5.5 0 0 1-.708-.708L10.293 8.5H4.5A.5.5 0 0 1 4 8"/>
</svg>
        `);



        ////////////////    event click navlink     ////////////////////    

        var navLinks = document.querySelectorAll('.nav-link');
        navLinks.forEach(function (navLink, id) {
            navLink.addEventListener('click', function (event) {
                event.preventDefault();
                let targetIndex;
                var targetId = this.getAttribute('href').substring(1);
                var targetElement = document.getElementById(targetId);
                if (targetElement) {
                    var numSlidesMove;
                    box.forEach(function (item, index) {
                        if ($(item).find('section').attr('id') == $(targetElement).attr('id')) {
                            numSlidesMove = indx - index;
                            targetIndex = index;

                            if (numSlidesMove > 0) {
                                for (let i = targetIndex; i <= indx; i++) {
                                    let defaultStatus = $($('section')[i]).attr('id') + "Animation";
                                    eval(defaultStatus)(undefined, undefined, numSlidesMove)
                                }
                            }

                            if (numSlidesMove < 0) {
                                for (let i = indx; i < targetIndex; i++) {
                                    let defaultStatus = $($('section')[i]).attr('id') + "Animation";
                                    eval(defaultStatus)(undefined, undefined, numSlidesMove)
                                }
                            }
                        }
                    })
                    for (var i = 0; i < box.length; i++) {
                        let position = Math.round(getScaleY_FromMatrix($(box[i]).css('transform')) / 100);
                        Anim = TweenLite.to(box[i], 0.0, { yPercent: position * 100 + 100 * numSlidesMove });
                    }


                    indx = targetIndex;

                    var navL = document.querySelector('.nav-item a[href*=' + targetId + ']');
                    if (navL) {
                        $('.nav-item').removeClass('active');
                        navL.closest('li').classList.add('active');
                    }
                }
            });
        });

        ////////////////    back to top     ////////////////////

        $('.back-to-top').click(function () {
            document.querySelector("a[href*=Home]").click()
        });


        var OneActionOneScroll = true;

        function GoTop() {
            for (var i = box.length - 1; i >= 0; i--) {
                box[i].anim = TweenLite.to(box[i], 0.7, { yPercent: i * 100, pause: true });
            }
        }
        function Go(e) {            
            if (OneActionOneScroll) {
                OneActionOneScroll = false;

                var SD = isNaN(e) ? e.wheelDelta || -e.detail : e;
                var activePage = $(box[indx]).find(".page").attr('id') + "Animation";
                var x = eval(activePage)(SD, indx, undefined);
                if (x === true) {
                    if (SD > 0 && indx > 0) {
                        if (!Anim || !Anim.isActive()) {
                            for (var i = 0; i < box.length; i++) {
                                let position = Math.round(getScaleY_FromMatrix($(box[i]).css('transform')) / 100);
                                Anim = TweenLite.to(box[i], 0.7, { yPercent: position * 100 + 100 });
                            }
                            indx--;
                            if (EndAnimateOfPage === false) EndAnimateOfPage = true;
                        }
                    }

                    else if (SD < 0 && indx < box.length - 1) {
                        if (!Anim || !Anim.isActive()) {
                            indx++;
                            for (var i = 0; i < box.length; i++) {
                                let position = Math.round(getScaleY_FromMatrix($(box[i]).css('transform')) / 100);
                                Anim = TweenLite.to(box[i], 0.7, { yPercent: position * 100 - 100 });
                            }
                            if (EndAnimateOfPage === true) EndAnimateOfPage = false;
                        }
                    }
                }

                navLinks.forEach(function (navLink) {
                    $(navLink).closest('.nav-item').removeClass('active')
                    if ($(navLink).attr('href').replace('#', '') == $(box[indx]).find(".page").attr('id')) {
                        $(navLink).closest('.nav-item').addClass('active')
                    }
                })
                setTimeout(function () {
                    OneActionOneScroll = true;
                }, 200);
            }
        }

        ///// call Go function by drag /////        
        function isTouchDevice() {
            return 'ontouchstart' in window || navigator.maxTouchPoints > 0;
        }
        if (isTouchDevice()) {
            console.log("Touch device detected. Activating drag functionality.");
            var d = document.createElement('div');
            document.querySelector('body').appendChild(d);
            Draggable.create(d, {
                trigger: ".box",
                type: 'y',
                minimumMovement: box.length,
                cursor: 'auto',
                onDrag: function () { var x = this.getDirection("start") == 'up' ? -1 : 1; Go(x); }
            });
        }
        /////////////////////////

        document.addEventListener("mousewheel", Go);

        document.addEventListener("DOMMouseScroll", Go);

        window.addEventListener("load", GoTop);

        ////////init scroll list ////////
        let depth = 0;
        let item = vScrollList.querySelector('.js-vertical-scroll-list-item');
        let items = vScrollList.querySelectorAll('.js-vertical-scroll-list-item');
        let itemHeight = parseFloat(getComputedStyle(items[1], null).height.replace("px", ""));
        $(vScrollList).css('height', itemHeight * 3);

        $(vScrollList)
            .on('ux.vScrollList.init', function () {
                items[1].classList.add('is-active');
                $(this).trigger('ux.vScrollList.setHeight');
                $(this).trigger('ux.vScrollList.onWheel');
            })
            .on('ux.vScrollList.setHeight', function () {
                let itemHeight = parseFloat(getComputedStyle(item, null).height.replace("px", ""));
                $(this).css('height', "100%");
            })

        ////////init b scroll list ////////

        let depthb = 0;
        let itemb = bScrollList.querySelector('.js-vertical-scroll-list-item');
        let itemsb = bScrollList.querySelectorAll('.js-vertical-scroll-list-item');
        let itemHeightb = parseFloat(getComputedStyle(itemsb[itemsb.length - 1], null).height.replace("px", ""));
        $(bScrollList).css('height', '100vh');

        $(bScrollList)
            .on('ux.bScrollList.init', function () {
                $(this).trigger('ux.bScrollList.setHeight');
                $(this).trigger('ux.bScrollList.onWheel');
            })
            .on('ux.bScrollList.setHeight', function () {
                let itemHeightb = parseFloat(getComputedStyle(itemb, null).height.replace("px", ""));

                $(this).css('height', itemHeightb * 1);
            })


        const HomeAnimation = (direct, indx, nav) => {
            var tl = new TimelineLite();
            if (nav >= 0) {
                animateBlockDefault();
                tl.to(".text-existed", 0, { y: "0%", ease: Power1.easeInOut })
                    .to(".text-removed", 0, { y: "100%", ease: Power1.easeInOut }, 0)
                    .to(".text-existed-s", 0, { y: "0%", ease: Power1.easeInOut }, 0)
                    .to(".text-removed-s", 0, { y: "0%", ease: Power1.easeInOut }, 0)
                    .call(() => {
                        HomeAnimationCompleted = false;
                    });
            }
            if (nav < 0) {
                animateBlock();
                tl.to(".text-existed", 0, { y: "-100%", ease: Power1.easeInOut })
                    .to(".text-removed", 0, { y: "0%", ease: Power1.easeInOut }, 0)
                    .to(".text-existed-s", 0, { y: "-100%", ease: Power1.easeInOut }, 0)
                    .to(".text-removed-s", 0, { y: "-100%", ease: Power1.easeInOut }, 0)
                    .call(() => {
                        HomeAnimationCompleted = true;
                    });
            }

            function animateBlock() {
                let ExpandDis = $(".home-page").next('.page-expand').height();
                tl.to(".home-page", 0.5, { y: (-1) * ExpandDis, ease: Power1.easeInOut });
                tl.to(".home-page-expand", 0.5, { y: (-1) * ExpandDis, ease: Power1.easeInOut }, 0);
            }
            function animateBlockDefault() {
                tl.to(".home-page", 0.5, { y: 0, ease: Power1.easeInOut });
                tl.to(".home-page-expand", 0.5, { y: 0, ease: Power1.easeInOut }, 0);
            }

            if (EndAnimateOfPage === true) {
                if (direct < 0) {
                    return true
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
                    animateBlock();
                    EndAnimateOfPage = true;
                }
            }
            if (direct > 0) {

                if (HomeAnimationCompleted && !EndAnimateOfPage) {
                    tl.to(".text-existed", 0.5, { y: "0%", ease: Power1.easeInOut })
                        .to(".text-removed", 0.5, { y: "100%", ease: Power1.easeInOut }, 0)
                        .to(".text-existed-s", 0.5, { y: "0%", ease: Power1.easeInOut }, 0)
                        .to(".text-removed-s", 0.5, { y: "0%", ease: Power1.easeInOut }, 0)
                        .call(() => {
                            HomeAnimationCompleted = false;
                        });
                }
                if (HomeAnimationCompleted && EndAnimateOfPage) {
                    animateBlockDefault();
                    EndAnimateOfPage = false;
                }
            }
            return false

        }

        const AboutAnimation = (direct, indx, nav) => {
            var tl = new TimelineLite();

            if (nav >= 0) {
                tl.to(".hor-wrap p:first-of-type", 0, { x: "50%", opacity: 1, ease: Power1.easeInOut })
                    .to(".hor-wrap p:last-of-type", 0, { x: "0", opacity: 0, ease: Power1.easeInOut }, 0);
                tl.to(".img-right", 0, { bottom: "50%", right: "0%", opacity: 0, ease: Power1.easeInOut })
                    .to(".img-left", 0, { bottom: "50%", left: "0%", opacity: 0, ease: Power1.easeInOut }, 0)
                    .to(".img-ct", 0, { y: "0%", scale: 1, opacity: 1, ease: Power1.easeInOut }, 0)
                    .to(".hor-wrap", 0, { bottom: "0%", opacity: 0, ease: Power1.easeInOut }, 0)
                    .to(".bg-img", 0, { scale: 1, ease: Power1.easeInOut }, 0);
                AboutAnimationCompleted1 = false;
                AboutAnimationCompleted2 = false;
                AboutAnimationCompleted3 = false;
            }
            if (nav < 0) {
                tl.to(".hor-wrap p:first-of-type", 0, { x: "50", opacity: 0, ease: Power1.easeInOut })
                    .to(".hor-wrap p:last-of-type", 0, { x: "100", opacity: 1, ease: Power1.easeInOut }, 0);
                tl.to(".img-right", 0, { right: "90%", opacity: 1, bottom: "calc(50% + 25px)", ease: Power1.easeInOut })
                    .to(".img-left", 0, { left: "80%", opacity: 1, bottom: "calc(50% - 25px)", ease: Power1.easeInOut }, 0)
                    .to(".img-ct", 0, { scale: 0.3, y: "-100%", opacity: 0, ease: Power1.easeInOut }, 0)
                    .to(".hor-wrap", 0, { bottom: "50%", opacity: 1, ease: Power1.easeInOut }, 0)
                    .to(".bg-img", 0, { scale: 2, ease: Power1.easeInOut }, 0);
                AboutAnimationCompleted1 = true;
                AboutAnimationCompleted2 = true;
                AboutAnimationCompleted3 = true;
            }

            function animateBlock() {
                let ExpandDis = $(".home-page").next('.page-expand').height();
                tl.to(".home-page", 0.5, { y: (-1) * ExpandDis, ease: Power1.easeInOut });
                tl.to(".home-page-expand", 0.5, { y: (-1) * ExpandDis, ease: Power1.easeInOut }, 0);
            }
            function animateBlockDefault() {
                tl.to(".home-page", 0.5, { y: 0, ease: Power1.easeInOut });
                tl.to(".home-page-expand", 0.5, { y: 0, ease: Power1.easeInOut }, 0);
            }

            if (direct < 0 && EndAnimateOfPage === true) {
                return true
            }
            if ((direct > 0) && (!AboutAnimationCompleted2 && !AboutAnimationCompleted1 && !AboutAnimationCompleted3)) {
                return true
            }
            if (direct < 0) {
                if (AboutAnimationCompleted3 && !AboutAnimationCompleted1 && !AboutAnimationCompleted2) {
                    tl.to(".hor-wrap p:first-of-type", 0.5, { opacity: 0, xPercent: '0', ease: Power1.easeInOut })
                        .to(".hor-wrap p:last-of-type", 0.5, { opacity: 1, xPercent: '-50', ease: Power1.easeInOut }, 0);
                    AboutAnimationCompleted3 = true;
                    AboutAnimationCompleted2 = true;
                    AboutAnimationCompleted1 = true;
                    EndAnimateOfPage = true;
                }

                if (AboutAnimationCompleted2 && !AboutAnimationCompleted1 && !AboutAnimationCompleted3) {
                    tl.to(".img-right", 0.5, { bottom: "calc(50% + 25px)", ease: Power1.easeInOut })
                        .to(".img-left", 0.5, { bottom: "calc(50% - 25px)", ease: Power1.easeInOut }, 0)
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
                if (!AboutAnimationCompleted1 && AboutAnimationCompleted2 && !AboutAnimationCompleted3) {
                    tl.to(".img-right", 0.5, { right: "0%", opacity: 0, ease: Power1.easeInOut })
                        .to(".img-left", 0.5, { left: "0%", opacity: 0, ease: Power1.easeInOut }, 0)
                        .to(".img-ct", 0.5, { scale: 1, y: 0, opacity: 1, ease: Power1.easeInOut }, 0)
                        .to(".hor-wrap", 0.5, { bottom: "0", opacity: 0, ease: Power1.easeInOut }, 0)
                        .to(".bg-img", 0.5, { scale: 1, ease: Power1.easeInOut }, 0);
                    AboutAnimationCompleted1 = false;
                    AboutAnimationCompleted2 = false;
                    AboutAnimationCompleted3 = false;
                }

                if (AboutAnimationCompleted3 && !AboutAnimationCompleted1 && !AboutAnimationCompleted2) {
                    tl.to(".img-right", 0.5, { bottom: "50%", ease: Power1.easeInOut })
                        .to(".img-left", 0.5, { bottom: "50%", ease: Power1.easeInOut }, 0)
                        .to(".img-ct", 0.5, { y: "0%", opacity: 1, ease: Power1.easeInOut }, 0)
                        .to(".hor-wrap", 0.5, { bottom: "-50%", opacity: 1, ease: Power1.easeInOut }, 0);
                    AboutAnimationCompleted1 = false;
                    AboutAnimationCompleted2 = true;
                    AboutAnimationCompleted3 = false;
                }

                if (AboutAnimationCompleted3 && AboutAnimationCompleted2 && AboutAnimationCompleted1) {
                    EndAnimateOfPage = false;
                    if (EndAnimateOfPage === false) {
                        tl.to(".hor-wrap p:first-of-type", 0.5, { opacity: 1, xPercent: '0', ease: Power1.easeInOut })
                            .to(".hor-wrap p:last-of-type", 0.5, { opacity: 0, xPercent: '0', ease: Power1.easeInOut }, 0);
                        AboutAnimationCompleted2 = false;
                        AboutAnimationCompleted1 = false;
                        AboutAnimationCompleted3 = true;

                    }
                }

            }
            return false
        }

        const ProAnimation = (direct, indx, nav) => {
            var tl = new TimelineLite();
            let ExpandDis = $(".pr-title-page").find('.page-expand').height();
            if (nav >= 0) {
                tl.to(".projects-title", 0, { scale: 1.3, ease: Power1.easeInOut })
                    .to(".about-page-expand", 0, { y: 0, ease: Power1.easeInOut }, 0)
                    .to(".projects-title", 0, { y: 0, ease: Power1.easeInOut }, 0)
                    .call(() => {
                        ProAnimationCompleted = false;
                    });
            }
            if (nav < 0) {
                tl.to(".projects-title", 0, { scale: 1, ease: Power1.easeInOut })
                    .to(".about-page-expand", 0, { y: (-1) * ExpandDis, ease: Power1.easeInOut }, 0)
                    .to(".projects-title", 0, { y: (-1) * ExpandDis, ease: Power1.easeInOut }, 0)
                    .call(() => {
                        ProAnimationCompleted = true;
                    })
            }

            if (direct == undefined) {
                ProAnimationCompleted = false;
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
                    return true
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

        const ProjectAnimation = (direct, indx, nav) => {
            var tl = new TimelineLite();

            function throttledUp(x) {
                for (var i = 0; i < numSliders.length; i++) {
                    let position = Math.round(getScaleY_FromMatrix($(numSliders[i]).css('transform')) / 100);
                    tl.to(numSliders[i], 0.7, { yPercent: position * 100 - 100 }, 0)

                    gsap.to($(numSliders[x + 1]).find(".paralax"), {
                        duration: 0.7,
                        backgroundPosition: 'center 100%',
                        ease: 'power1.inOut'
                    })
                    gsap.to($(numSliders[x]).find(".paralax"), {
                        duration: 0.7,
                        backgroundPosition: 'center 60%',
                        ease: 'power1.inOut'
                    });
                }
            };

            function throttledDown(x) {
                for (var i = 0; i < numSliders.length; i++) {
                    let position = Math.round(getScaleY_FromMatrix($(numSliders[i]).css('transform')) / 100);
                    tl.to(numSliders[i], 0.7, { yPercent: position * 100 + 100 }, 0);

                    gsap.to($(numSliders[x + 1]).find(".paralax"), {
                        duration: 0.7,
                        backgroundPosition: 'center 60%',
                        ease: 'power1.inOut'
                    })
                    gsap.to($(numSliders[x]).find(".paralax"), {
                        duration: 0.7,
                        backgroundPosition: 'center 100%',
                        ease: 'power1.inOut'
                    });

                }
            };


            if (nav >= 0) {
                if (numS != 0) {
                    for (let i = 0; i < numSliders.length; i++) {
                        let position = Math.round(getScaleY_FromMatrix($(numSliders[i]).css('transform')) / 100);
                        tl.to(numSliders[i], 0, { yPercent: 0 }, 0);

                        gsap.to($(numSliders[i + 1]).find(".paralax"), {
                            duration: 0,
                            backgroundPosition: 'center 60%',
                            ease: 'power1.inOut'
                        })
                        gsap.to($(numSliders[i]).find(".paralax"), {
                            duration: 0,
                            backgroundPosition: 'center 100%',
                            ease: 'power1.inOut'
                        });
                    }
                }
                tl.to(".see-all-projects", 0, { bottom: '-100%', ease: Power1.easeInOut }, 0);
                if (numS === numSliders.length - 1) horizontalBlindsReverse("#last-item", 0);
                numS = 0;
                ProjectAnimationCompleted1 = false;
                ProjectAnimationCompleted2 = false;
            }


            if (nav < 0) {
                for (let i = numS; i < numSliders.length; i++) {
                    let position = Math.round(getScaleY_FromMatrix($(numSliders[i]).css('transform')) / 100);
                    tl.to(numSliders[i], 0, { yPercent: position * 100 - 100 * (numSliders.length - 1 - numS) }, 0)
                    gsap.to($(numSliders[i + 1]).find(".paralax"), {
                        duration: 0,
                        backgroundPosition: 'center 100%',
                        ease: 'power1.inOut'
                    })
                    gsap.to($(numSliders[i]).find(".paralax"), {
                        duration: 0,
                        backgroundPosition: 'center 60%',
                        ease: 'power1.inOut'
                    });
                }
                horizontalBlinds("#last-item", 0);
                numS = numSliders.length - 1;
                ProjectAnimationCompleted1 = true;
                ProjectAnimationCompleted2 = true;
            }


            if (direct < 0 && EndAnimateOfPage === true) {
                return true
            }
            if (direct > 0 && EndAnimateOfPage === false && numS === 0) {
                return true
            }


            if (direct < 0) {
                if (numS === numSliders.length - 1) {
                    ProjectAnimationCompleted1 = true;
                }
                if (numS < numSliders.length - 1 && !ProjectAnimationCompleted2) {
                    throttledUp(numS);
                    numS++;
                }
                if (ProjectAnimationCompleted1) {
                    if (ProjectAnimationCompleted2) {
                        horizontalBlinds("#last-item", 100);
                        EndAnimateOfPage = true;
                    }
                    tl.to(".see-all-projects", 0.3, { bottom: '50px', ease: Power1.easeInOut });
                    ProjectAnimationCompleted2 = true;
                }
            }

            if (direct > 0) {
                console.log(EndAnimateOfPage, numS, direct)

                if (numS > 0 && !ProjectAnimationCompleted1 && !ProjectAnimationCompleted2) {
                    numS--;
                    throttledDown(numS);
                }

                if (EndAnimateOfPage) {
                    if (ProjectAnimationCompleted2) {
                        horizontalBlindsReverse("#last-item", 100);
                        tl.to(".see-all-projects", 0.3, { bottom: '50px', ease: Power1.easeInOut }, 0);
                        EndAnimateOfPage = false;
                    }
                }
                else {
                    if (ProjectAnimationCompleted2) {
                        ProjectAnimationCompleted2 = false;
                    }
                }

                if (ProjectAnimationCompleted1 && numS === numSliders.length - 1 && !ProjectAnimationCompleted2) {
                    tl.to(".see-all-projects", 0.3, { bottom: '-100%', ease: Power1.easeInOut });
                    ProjectAnimationCompleted1 = false;

                }

            }
            return false
        }

        const Services1Animation = (direct, indx, nav) => {
            var tl = new TimelineLite();

            if ((direct < 0 && EndAnimateOfPage === true) || (direct > 0 && EndAnimateOfPage === false && depth === 0)) return true;

            const setActiveItem = (direct) => {
                for (let i = 0; i < items.length; i++) {
                    if (items[i].classList.contains('is-active')) {
                        items[i].classList.remove('is-active');
                        (direct < 0) ? items[i + 1].classList.add('is-active') : items[i - 1].classList.add('is-active');
                        break;
                    }
                }
            }

            let list = vScrollList.querySelector('ul');

            if (nav >= 0) {
                depth = 0;
                tl.to(list, 0, { y: 0, ease: Power1.easeOut }, 0);
                for (let i = 0; i < items.length; i++) {
                    if (items[i].classList.contains('is-active')) {
                        items[i].classList.remove('is-active');
                    }
                }
                items[1].classList.add('is-active');
            }

            if (nav < 0) {
                depth = items.length - 3;
                tl.to(list, 0, { y: -itemHeight * depth, ease: Power1.easeOut }, 0);
                for (let i = 0; i < items.length; i++) {
                    if (items[i].classList.contains('is-active')) {
                        items[i].classList.remove('is-active');
                    }
                }
                items[items.length - 2].classList.add('is-active');
            }

            if (direct < 0 && depth < items.length - 3) {
                depth += 1;
                let value = -itemHeight * depth;
                tl.to(list, 0.3, { y: value, ease: Power1.easeOut }, 0);
                setActiveItem(direct);
            }

            if (direct > 0 && depth > 0) {
                depth -= 1;
                let value = -itemHeight * depth;
                tl.to(list, 0.3, { y: value, ease: Power1.easeOut }, 0);
                setActiveItem(direct);
            }
            if (direct < 0 && depth == items.length - 3) {
                EndAnimateOfPage = true;
            }
            if (direct > 0 && EndAnimateOfPage) {
                EndAnimateOfPage = false;
            }
            return false
        }

        const Services2Animation = (direct, indx, nav) => {

            var tl = new TimelineLite();

            let list = bScrollList.querySelector('ul');

            if (nav >= 0) {
                depthb = 0;
                tl.to(list, 0, { y: 0, ease: Power1.easeOut, ease: Power1.easeOut })
                    .to($('.wrapper-title'), 0, { yPercent: 0, ease: Power1.easeOut }, 0)
                    .to($('.vertical-scroll-list-more-page'), 0, { yPercent: 0, ease: Power1.easeOut }, 0);
                ServicesAnimationCompleted = false;
            }

            if (nav < 0) {
                depthb = itemsb.length - 1;
                tl.to(list, 0, { y: -itemHeightb * depthb, ease: Power1.easeOut, ease: Power1.easeOut })
                    .to($('.wrapper-title'), 0, { yPercent: -100, ease: Power1.easeOut }, 0)
                    .to($('.vertical-scroll-list-more-page'), 0, { yPercent: -100, ease: Power1.easeOut }, 0);
                ServicesAnimationCompleted = true;
            }

            if (direct > 0 && depthb == 0) { ServicesAnimationCompleted = false };

            if (bScrollList !== null && ServicesAnimationCompleted) {

                if ((direct > 0 && depthb == 0 && !EndAnimateOfPage) || (direct < 0 && depthb == itemsb.length - 1 && EndAnimateOfPage)) {
                    return true
                }

                if (direct < 0 && depthb < itemsb.length - 1) {
                    depthb++;
                    let value = -itemHeightb * depthb;
                    tl.to(list, 0.7, { y: value, ease: Power1.easeOut, ease: Power1.easeOut });
                }

                if (direct < 0 && depthb == itemsb.length - 1) {
                    EndAnimateOfPage = true;
                }

                if (direct > 0 && depthb > 0) {
                    let value = -itemHeightb * (depthb - 1);
                    tl.to(list, 0.7, { y: value, ease: Power1.easeOut, ease: Power1.easeOut });
                    depthb--;
                }
                if (direct > 0 && depthb <= itemsb.length - 1) {
                    EndAnimateOfPage = false;
                }

            }

            if (!ServicesAnimationCompleted) {
                if (direct < 0) {
                    let position = Math.round(getScaleY_FromMatrix($('.wrapper-title').css('transform')) / 100);
                    tl.to($('.wrapper-title'), 0.7, { yPercent: position * 100 - 100, ease: Power1.easeOut }, 0);
                    position = Math.round(getScaleY_FromMatrix($('.vertical-scroll-list-more-page').css('transform')) / 100);
                    tl.to($('.vertical-scroll-list-more-page'), 0.7, { yPercent: position * 100 - 100, ease: Power1.easeOut }, 0);
                    ServicesAnimationCompleted = true;
                }
                if (direct > 0) {
                    let position = Math.round(getScaleY_FromMatrix($('.wrapper-title').css('transform')) / 100);
                    if (position == 0) {
                        return true
                    }
                    tl.to($('.wrapper-title'), 0.7, { yPercent: position * 100 + 100, ease: Power1.easeOut }, 0);
                    position = Math.round(getScaleY_FromMatrix($('.vertical-scroll-list-more-page').css('transform')) / 100);
                    tl.to($('.vertical-scroll-list-more-page'), 0.7, { yPercent: position * 100 + 100, ease: Power1.easeOut }, 0);
                }
            }

            return false
        }

        const ClientsAnimation = (direct, indx, nav) => {
            var tl = new TimelineLite();
            if (nav >= 0) {
                tl.to(".contact-page-form", 0, { x: "0%", opacity: 1, ease: Power1.easeInOut })
                    .to(".contact-page-try", 0, { x: "0%", opacity: 0, ease: Power1.easeInOut }, 0)
                    .call(() => {
                        ContactAnimationCompleted = false;
                    })
            }
            if (nav < 0) {
                tl.to(".contact-page-form", 0, { x: "-100%", opacity: 0, ease: Power1.easeInOut })
                    .to(".contact-page-try", 0, { x: "-100%", opacity: 1, ease: Power1.easeInOut }, 0)
                    .call(() => {
                        ContactAnimationCompleted = true;
                    })
            }
            if (direct > 0 && !ContactAnimationCompleted) {
                return true
            }
            if (direct < 0) {
                if (!ContactAnimationCompleted) {
                    tl.to(".contact-page-form", 0.7, { x: "-100%", opacity: 0, ease: Power1.easeInOut })
                        .to(".contact-page-try", 0.7, { x: "-100%", opacity: 1, ease: Power1.easeInOut }, 0)
                        .call(() => {
                            ContactAnimationCompleted = true;
                        })
                } else {
                    EndAnimateOfPage = true;
                    return true
                }
            }
            if (direct > 0) {
                if (ContactAnimationCompleted) {
                    tl.to(".contact-page-form", 0.7, { x: "0%", opacity: 1, ease: Power1.easeInOut })
                        .to(".contact-page-try", 0.7, { x: "0%", opacity: 0, ease: Power1.easeInOut }, 0)
                        .call(() => {
                            ContactAnimationCompleted = false;
                        })
                }
                else {
                    EndAnimateOfPage = false;
                }
            }
            return false
        }

        const ContactAnimation = (direct, indx, nav) => {

            var tl = new TimelineLite();

            if (direct > 0) {
                return true
            }
            return false
        }
    });

    //////////////// slats blind ////////////////////////////
    var slatCount = 10;

    function horizontalBlinds(elementSelector, animationDuration) {
        var slatHeight = $(window).height() * (slatCount / 100);
        var horizalPieces = $(elementSelector + " > .slat");

        for (var count = 0; count < slatCount; count++) {
            var newSlat = $('<div class="slat quickflip"></div>');
            $(elementSelector).children().clone().appendTo(newSlat);
            $(elementSelector).append(newSlat);
            newSlat.css("height", slatHeight);
        }

        $(elementSelector + " .projects-detail-item-content").css('opacity', '0');
        $(".see-all-projects").hide();
        $("#last-item").css('outlineOffset', '-2px');
        $("#last-item").css('outlineWidth', '3px');


        $(elementSelector + ">.paralax").css('display', 'none');
        var horizalPieces = $(elementSelector + " > .slat");

        let countBlind = 0;

        function checkCallback() {
            countBlind++;
            if (countBlind == slatCount) {
                horizalPieces.hide()
            }
        }

        for (let x = horizalPieces.length - 1; x >= 0; x--) {
            slatBlinds(x, checkCallback);
        }

        function slatBlinds(x, callback) {
            $(horizalPieces[x]).find(".paralax").css('background-position', `center ${(x * (-1) * slatHeight)}px`)
            setTimeout(function () {
                var tl = new TimelineLite();
                gsap.set(".quickflip", {
                    transformStyle: "preserve-3d",
                    transformPerspective: 1000
                });
                $(horizalPieces[x]).css("overflow", "hidden");
                tl.to(horizalPieces[x], { rotationX: -90, duration: 0.5 })
                    .call(() => {
                        if (callback) callback();
                    })
            }, (slatCount - x) * animationDuration)
        }
    }

    function horizontalBlindsReverse(elementSelector, animationDuration) {
        var horizalPieces = $(elementSelector + " > .slat");
        if (horizalPieces) {
            horizalPieces.show();

            let countBlind = 0;

            function checkCallback() {
                countBlind++;
                if (countBlind == slatCount) {
                    $(elementSelector + ">.paralax").css('display', 'block');
                    $(elementSelector + " .projects-detail-item-content").css('opacity', '1');
                    $(elementSelector + ">.slat").css('overflow', 'unset');
                    $(".see-all-projects").show();
                    $("#last-item").css('outline-offset', '0px');
                    $("#last-item").css('outline-width', '0px');
                    horizalPieces.remove();
                }
            }

            for (let x = 0; x < horizalPieces.length; x++) {
                slatBlindsReverse(x, checkCallback);
            }

        }
        function slatBlindsReverse(x, callback) {
            setTimeout(function () {
                var tl = new TimelineLite();
                gsap.set(".quickflip", {
                    transformStyle: "preserve-3d",
                    transformPerspective: 1000
                });
                tl.to(horizalPieces[x], { rotationX: 0, duration: 0.5 })
                    .call(() => {
                        if (callback) callback();
                    })
            }, x * animationDuration);


        }
    }
});







