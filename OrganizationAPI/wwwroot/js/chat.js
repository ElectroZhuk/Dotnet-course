"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/organizationHub").build();

//Disable the send button until connection is established.
document.getElementById("getButton").disabled = true;

var messages = [];

connection.on("ReceiveMessageFromSystem", function (message) {
    messages.push(message);
});

connection.start().then(function () {
    document.getElementById("getButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("getButton").addEventListener("click", function (event) {
    var messagesList = document.getElementById("messagesList");

    for (let i = 0; i <= messages.length - 1; i++) {
        var li = document.createElement("li");
        messagesList.appendChild(li);
        li.textContent = messages[i];
    }

    messages.length = 0;
    event.preventDefault();
});