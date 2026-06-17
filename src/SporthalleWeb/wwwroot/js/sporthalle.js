document.getElementById("menu-toggle").addEventListener("click", function () {
  const navLinks = document.getElementById("nav-links");
  navLinks.classList.toggle("active");
});

const navLinks = document.querySelectorAll("#nav-links a");
navLinks.forEach((link) => {
  link.addEventListener("click", function () {
    const nav = document.getElementById("nav-links");
    nav.classList.remove("active");
  });
});
