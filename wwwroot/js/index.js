const origin = window.location.origin;

const secretMessage = document.getElementById("secret-message");

fetch(origin + "/home/secret").then(resp => {
    resp.text().then(body => {
        secretMessage.innerText = body;
    })
});