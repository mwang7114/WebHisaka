function toggleSidebar() {
    var sidebar = document.getElementById("sidebar");
    var header = document.querySelector(".header");
    var mainContent = document.querySelector(".main-content");

    // Kiểm tra xem sidebar có đang ẩn hay không
    if (sidebar.style.display === "none") {
        sidebar.style.display = "block"; // Hiển thị sidebar
        header.classList.remove("sidebar-hidden"); // Hiển thị lại header với margin
        mainContent.classList.remove("sidebar-hidden"); // Hiển thị lại main-content với margin
    } else {
        sidebar.style.display = "none"; // Ẩn sidebar
        header.classList.add("sidebar-hidden"); // Ẩn header
        mainContent.classList.add("sidebar-hidden"); // Ẩn main-content
    }
}



document.addEventListener("DOMContentLoaded", function () {
    const sidebarItems = document.querySelectorAll(".sidebar-item a");
    let currentPath = window.location.pathname.split('?')[0]; // Lấy đường dẫn hiện tại

    // Lấy giá trị `active` từ `localStorage` nếu có
    const activePath = localStorage.getItem("activePath");

    // Duyệt qua tất cả các mục sidebar
    sidebarItems.forEach((item) => {
        const linkPath = item.getAttribute("href").split('?')[0];

        // Nếu đường dẫn trong sidebar trùng với đường dẫn active trong localStorage hoặc đường dẫn hiện tại, gán active
        if (linkPath === activePath || currentPath === linkPath) {
            item.parentElement.classList.add("active");
        }
    });

    // Đảm bảo không tự động thay đổi active nếu người dùng không chọn mục trong sidebar
    if (!document.querySelector(".sidebar-item.active") && activePath) {
        sidebarItems.forEach((item) => {
            const linkPath = item.getAttribute("href").split('?')[0];
            // Giữ nguyên active nếu có
            if (linkPath === activePath) {
                item.parentElement.classList.add("active");
            }
        });
    }

    // Lắng nghe sự kiện khi người dùng nhấp vào sidebar để thay đổi activePath
    sidebarItems.forEach((item) => {
        item.addEventListener("click", function () {
            const linkPath = item.getAttribute("href").split('?')[0];
            localStorage.setItem("activePath", linkPath); // Lưu trạng thái active vào localStorage
        });
    });
});








