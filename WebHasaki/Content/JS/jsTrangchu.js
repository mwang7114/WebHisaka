
var myIndex = 0;
var slideInterval;

function carousel() {
    var i;
    var x = document.getElementsByClassName("mySlides");
    for (i = 0; i < x.length; i++) {
        x[i].style.display = "none";
    }
    myIndex++;
    if (myIndex > x.length) { myIndex = 1 }
    x[myIndex - 1].style.display = "block";
}

function startCarousel() {
    slideInterval = setInterval(carousel, 2000); // Change image every 2 seconds
}

function plusDivs(n) {
    clearInterval(slideInterval); // Dừng tự chuyển khi người dùng bấm nút
    myIndex += n;
    if (myIndex > document.getElementsByClassName("mySlides").length) { myIndex = 1 }
    if (myIndex < 1) { myIndex = document.getElementsByClassName("mySlides").length }
    showDivs(myIndex);
    startCarousel(); // Bắt đầu lại tự chuyển
}

function currentDiv(n) {
    clearInterval(slideInterval); // Dừng tự chuyển khi người dùng bấm vào badge
    showDivs(myIndex = n);
    startCarousel(); // Bắt đầu lại tự chuyển
}

function showDivs(n) {
    var i;
    var x = document.getElementsByClassName("mySlides");
    for (i = 0; i < x.length; i++) {
        x[i].style.display = "none";
    }
    x[n - 1].style.display = "block";
}

// Hiển thị slide đầu tiên và khởi động carousel khi trang được tải
window.onload = function () {
    showDivs(myIndex = 1); // Hiển thị slide đầu tiên ngay lập tức
    startCarousel(); // Khởi động carousel
};
