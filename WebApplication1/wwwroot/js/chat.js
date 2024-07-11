"use strict";



//Disable the send button until connection is established.
document.getElementById("sendButton").disabled = true;

document.getElementById("groupIdInput").value = 1505;
const userId = Math.floor(Math.random() * 100000);
let requestedDate = new Date();
let requestedGroupId = 1505; 
var deliveriesList = [];
document.getElementById("dateChoice").value = requestedDate.toISOString().split('T')[0]
console.log(document.getElementById("dateChoice").value)
document.getElementById("userIdLabel").appendChild(document.createTextNode(`User id: ${userId}`))


var connection = new signalR.HubConnectionBuilder().withUrl(
    `/deliveryPlanner?date=${requestedDate.toDateString()}&groupid=1505&userid=${userId}`).build();


connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
    alert("Connected");
}).catch(function (err) {
    return console.error(err.toString());
});


connection.on("NewDeliveryCreated", function (delivery) {
    createDivWithButtons(delivery.id, delivery.createdByUserId);
    deliveriesList.push(delivery);
})

connection.on("Disconnect", () =>{
    
    connection.invoke("DisconnectRequested").catch(function (err) {
        return console.error(err.toString());
    });
    alert("You were disconnected due to inactivity");
});

connection.on("MonitoringUpdate", (overview) => {
    const containerDiv = document.getElementById("monitoringInfo");
    containerDiv.replaceChildren();
    const numUserDiv = document.createElement("div");
    let textNode = document.createTextNode(`Number of users online:${overview.numberOfConnectedUsers}`);
    numUserDiv.appendChild(textNode);
    const numSessionsDiv = document.createElement("div");
    textNode = document.createTextNode(`Number of sessions: ${overview.numberOfSessions}`);
    numSessionsDiv.appendChild(textNode);
    const numDeliveriesDiv = document.createElement("div");
    textNode = document.createTextNode(`Number of deliveries in all sessions:${overview.numberOfDeliveries}`);
    numDeliveriesDiv.appendChild(textNode);
    containerDiv.appendChild(numUserDiv);
    containerDiv.appendChild(numSessionsDiv);
    containerDiv.appendChild(numDeliveriesDiv);
});

function onIdentityUpdated(){
    const groupId = parseInt(document.getElementById("groupIdInput").value);
    const date = document.getElementById("dateChoice").value;
    
    if(isValidDate(date) && isValidNumber(groupId) && connection.state === signalR.HubConnectionState.Connected){
        connection.invoke("IdentityUpdate", date, requestedDate, groupId, requestedGroupId, userId).then(
            requestedGroupId = groupId, requestedDate = date
        ).catch(function (err) {
            return console.error(err.toString());
        })
        
    }
}




connection.on("IdentityUpdated", function (deliveries) {
    document.getElementById("deliveriesList").replaceChildren();
    deliveries.sort((a, b) => a.priority - b.priority);
    deliveriesList = deliveries;
    for(let i = 0; i < deliveries.length; i++){
        createDivWithButtons(deliveries[i].id, deliveries[i].createdByUserId);
    }
})

connection.on("DeliveryRemoved", (delivery) => {
    document.getElementById(`delivery-${delivery.id}`).remove();
    const index = deliveriesList.index(e => e.id === delivery.id);
    if (index !== -1){
        deliveriesList.splice(index, 1);
    }
    console.log(deliveriesList)
});


connection.onclose(() => {
    document.getElementById("sendButton").disabled = true;
    alert("The connection is closed. Reload the page to start again.")
})

document.getElementById("sendButton").addEventListener("click",function (event) {
    const groupId = parseInt(document.getElementById("groupIdInput").value);
    const date = document.getElementById("dateChoice").value;
    const id = Math.floor(Math.random() * 100000);
    let delivery = { id : id, CreatedByUserId: userId };

    if(isValidDate(date) && isValidNumber(groupId) && connection.state === signalR.HubConnectionState.Connected){
        connection.invoke("CreateDelivery",date, groupId , userId, delivery).catch(function (err) {
            return console.error(err.toString());
        });
        
    }
    event.preventDefault();
});




document.getElementById("dateChoice").addEventListener("change", function (event) {
    onIdentityUpdated();
    event.preventDefault();
});

document.getElementById("groupIdInput").addEventListener("change", function (event) {
    onIdentityUpdated();
    event.preventDefault();
});


function createDivWithButtons(id, CreatedByUserId) {
    // Create container div
    const containerDiv = document.createElement('div');
    containerDiv.id = `delivery-${id}`
    containerDiv.classList.add('container');

    // Create text node
    const textNode = document.createTextNode(`Delivery ${id}, created by: ${CreatedByUserId}`);
    containerDiv.appendChild(textNode);
    
    // Create buttons
    const removeButton = createButton('remove', removeDiv);
    const moveUpButton = createButton('move up', moveUp);
    const moveDownButton = createButton('move down', moveDown);
    
    containerDiv.appendChild(removeButton);
    containerDiv.appendChild(moveUpButton);
    containerDiv.appendChild(moveDownButton);
    
    // Append container div to parent div
    document.getElementById('deliveriesList').appendChild(containerDiv);
}

function createButton(text, clickHandler) {
    const button = document.createElement('button');
    button.classList.add('btn', 'btn-primary', 'm-1')
    button.textContent = text;
    button.addEventListener('click', clickHandler);
    return button;
}

function removeDiv(event) {
    const groupId = parseInt(document.getElementById("groupIdInput").value);
    const date = document.getElementById("dateChoice").value;

    const containerDiv = event.target.parentElement;
    const regex = /-(\d+)/;
    const match = containerDiv.id.match(regex);
    
    if (!match) {
        alert("Cannot delete delivery due to lack of id.");
        return;
    }
    let delivery = { id : parseInt(match[1]), CreatedByUserId: userId };
    
    if(isValidDate(date) && isValidNumber(groupId) && connection.state === signalR.HubConnectionState.Connected){
        console.log(groupId)
        console.log(userId)
        
        connection.invoke("RemoveDelivery", new Date(date),  groupId, userId, delivery).catch(function (err) {
            return console.error(err.toString());
        })
    }
    
}

function moveUpDelivery(event){
    const containerDiv = event.target.parentElement;
    const regex = /-(\d+)/;
    const matchFirst = containerDiv.id.match(regex);

    if (!matchFirst) {
        alert("Cannot delete delivery due to lack of id.");
        return;
    }
    
    const secondContainerDiv = containerDiv.previousElementSibling;
    if (secondContainerDiv === null){
        return;
    }
    
    const matchSecond = secondContainerDiv.id.match(regex);

    if (!matchSecond) {
        alert("Cannot delete delivery due to lack of id.");
        return;
    }
    
    const firstDelivery =  deliveriesList.find(e => e.id === parseInt(matchFirst[1]));
    const secondDelivery =  deliveriesList.find(e => e.id === parseInt(matchSecond[1]));

    if (firstDelivery && secondDelivery){
        swapDeliveries(firstDelivery, secondDelivery);
    }
}

function moveDownDelivery(event){
    const containerDiv = event.target.parentElement;
    const regex = /-(\d+)/;
    const matchFirst = containerDiv.id.match(regex);

    if (!matchFirst) {
        alert("Cannot delete delivery due to lack of id.");
        return;
    }
        
    const secondContainerDiv = containerDiv.nextElementSibling;
    if (secondContainerDiv === null){
        return;
    }

    const matchSecond = secondContainerDiv.id.match(regex);

    if (!matchSecond) {
        alert("Cannot delete delivery due to lack of id.");
        return;
    }
    
    const firstDelivery =  deliveriesList.find(e => e.id === parseInt(matchFirst[1]));
    const secondDelivery =  deliveriesList.find(e => e.id === parseInt(matchSecond[1]));
   
    
    if (firstDelivery && secondDelivery){
        swapDeliveries(firstDelivery, secondDelivery);
    } 
    
}


function swapDeliveries(deliveryFirst, deliverySecond){
    const groupId = parseInt(document.getElementById("groupIdInput").value);
    const date = document.getElementById("dateChoice").value;
   
    if(isValidDate(date) && isValidNumber(groupId) && connection.state === signalR.HubConnectionState.Connected){
        connection.invoke("UpdatePriorities",date, groupId , userId, deliveryFirst, deliverySecond).catch(function (err) {
            return console.error(err.toString());
        });

    }
    
}



function moveUp(event) {
    moveUpDelivery(event);
}

function moveDown(event) {
    moveDownDelivery(event)
}

function isValidNumber(input) {
    return !isNaN(input);
}

function isValidDate(input) {
    const date = new Date(input);
    return date instanceof Date && !isNaN(date);
}


