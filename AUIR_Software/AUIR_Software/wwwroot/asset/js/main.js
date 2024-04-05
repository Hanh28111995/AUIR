gsap.registerPlugin(ScrollTrigger);

/////////////////// click to scroll next page /////////////////////////
var fix_for_click = 35;

$(document).ready(function () {
    $(".section").scroll(function ()
    {
        const dataActionValue = this.getAttribute('data-action');
        const idValue = this.getAttribute('id');
        console.log(idValue);
        if (dataActionValue == 0 ) {
            if (idValue == 'Home') {
            HomeAnimation(this);
                this.setAttribute('data-action', 1);
            }
            if (idValue == 'About') {
                AboutAnimation(this);                
            }
        }        
        else {
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
        }
    });
});


////////////////event click navlink ////////////////////

document.addEventListener('DOMContentLoaded', function () {
    var navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(function (navLink) {
        navLink.addEventListener('click', function (event) {
            event.preventDefault();
            var targetId = this.getAttribute('href').substring(1);
            console.log(targetId)
            var targetElement = document.getElementById(targetId);
            if (targetElement) {
                var targetOffsetTop = targetElement.offsetTop - 150;
                var element = document.getElementById("section");
                element.scrollTo({
                    top: targetOffsetTop,
                    behavior: 'smooth'
                });

                var navL = document.querySelector('.nav-item a[href*=' + targetId + ']');
                if (navL) {
                    $('.nav-item').removeClass('active');
                    navL.closest('li').classList.add('active');
                }
            }
        });
    });
});



////////////gsap setting //////////////////////////////
function HomeAnimation(ele) {    
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
    })
}

function AboutAnimation(ele) {
    gsap.to(".img-right", {
        right: "90%" , delay: 0, duration: 1.5, opacity: 1
    })
    gsap.to(".img-left", {
        left: "80%", delay: 0, duration: 1.5, opacity: 1
    })
    gsap.to(".img-ct", {
        scale: 0.3, delay: 0, duration: 1.5
    })
    gsap.to(".bg-img", {
        scale: 2, delay: 0, duration: 1.5,
        onComplete: function () {
            ele.addEventListener("click", function () {
                gsap.to(".img-right", {
                    top: "45%", delay: 0, duration: 1, 
                })
                gsap.to(".img-left", {
                    top: "55%", delay: 0, duration: 1,
                })                
                gsap.to(".img-ct", {
                    y: "-100%", delay: 0, opacity: 0, duration: 1,                    
                })
                gsap.to(".hor-wrap", {
                    bottom: "50%", delay: 0, opacity: 1, duration: 1,
                    onComplete: function () {
                        ele.addEventListener("click", function () {
                            gsap.to(".hor-wrap p:first-of-type", {
                                x: "-100%", delay: 0, opacity: 0, duration: 1
                            })                            
                            gsap.to(".hor-wrap p:last-of-type", {
                                x: "0%", delay: 0, opacity: 1, duration: 1
                            })
                        })
                        /*ele.setAttribute('data-action', 1);*/
                    }
                })
            })
        }
    })
}


//////////////// slats blind ////////////////////////////
var slatCount = 10;

function horizontalBlinds(elementSelector) {
    var containerHeight = $(elementSelector).height();
    var slatHeight = containerHeight / slatCount;

    for (var count = 0; count < slatCount; count++) {
        var newSlat = $('<div class="slat"></div>');
        $(elementSelector).children().clone().appendTo(newSlat);
        $(elementSelector).append(newSlat);
        newSlat.css("height", slatHeight);
    }

    $(elementSelector + ">.paralax").css('display', 'none');
    $(elementSelector + " .projects-detail-item-content").hide();
    $(".see-all-projects").hide();

    var horizalPieces = $(elementSelector + " > .slat");
    var animationDuration = 100; // Adjust as needed
    for (let x = horizalPieces.length - 1; x >= 0; x--) {
        slatBlinds(x);
    }

    function slatBlinds(x) {

        setTimeout(function () {
            $(horizalPieces[x]).animate({ top: (x + 1) * (-10) + "%" }, (slatCount - (x + 1)) * animationDuration);
        }, (slatCount - x) * animationDuration)
    }
}

$(document).ready(function () {
    $('#blindbox').on('click', function () {
        horizontalBlinds('#blindbox');
    });
});


