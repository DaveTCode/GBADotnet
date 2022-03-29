﻿"use strict";

const connection = new signalR.HubConnectionBuilder().withUrl("/emulatorhub").build();

var backingCanvas;
var screenCanvas
var backingContext;
var screenContext;

/**
 * When a frame is generated by the backing application it will use signalr to 
 * trigger this function with a base 64 string version of the frame data.
 * 
 * This function is then responsible for drawing onto the canvas (and must 
 * obviously take less than 16ms or it will be called again in the interim 
 * period)
 */
connection.on("SendFrame", function (data) {
    if (!backingCanvas) backingCanvas = document.querySelector("#backing-canvas");
    if (!screenCanvas) screenCanvas = document.querySelector("#screen-canvas");
    if (!backingContext) backingContext = backingCanvas.getContext("2d");
    if (!screenContext) screenContext = screenCanvas.getContext("2d");

    const frameBytes = atob(data);
    const arr = new Uint8ClampedArray(frameBytes.length);

    for (let ii = 0; ii < arr.length; ii++) {
        if (ii % 4 == 3) {
            arr[ii] = 255;
        } else {
            arr[ii] = frameBytes.charCodeAt(ii);
        }
    }

    // Use a backing canvas to store the original unscaled data and then scale 
    // up by 2 * to get better resolution
    const imageData = new ImageData(arr, 240);
    backingContext.putImageData(imageData, 0, 0);
    screenContext.drawImage(backingCanvas, 0, 0, 240, 160, 0, 0, 480, 320);
});

/**
 * When the device is paused it will send the entire device object serialized 
 * so that we can pull interesting debug information out
 */
connection.on("SendDevice", function (device) {
    for (var ii = 0; ii < 16; ii++) {
        document.querySelector(`#cpu-r${ii}-td`).innerHTML = device.cpu.r[ii].toString(16);
    }

    document.querySelector("#dispcnt-td").innerHTML = JSON.stringify(device.ppu.dispcnt);
    document.querySelector("#greenswap-td").innerHTML = device.ppu.greenSwap.toString(16);
    document.querySelector("#dispstat-td").innerHTML = JSON.stringify(device.ppu.dispstat);
    document.querySelector("#vcount-td").innerHTML = device.ppu.currentLine;
    document.querySelector("#bg0cnt-td").innerHTML = JSON.stringify(device.ppu.backgrounds[0].control);
    document.querySelector("#bg1cnt-td").innerHTML = JSON.stringify(device.ppu.backgrounds[1].control);
    document.querySelector("#bg2cnt-td").innerHTML = JSON.stringify(device.ppu.backgrounds[2].control);
    document.querySelector("#bg3cnt-td").innerHTML = JSON.stringify(device.ppu.backgrounds[3].control);
    document.querySelector("#bg0hofs-td").innerHTML = device.ppu.backgrounds[0].xOffset;
    document.querySelector("#bg0vofs-td").innerHTML = device.ppu.backgrounds[0].yOffset;
    document.querySelector("#bg1hofs-td").innerHTML = device.ppu.backgrounds[1].xOffset;
    document.querySelector("#bg1vofs-td").innerHTML = device.ppu.backgrounds[1].yOffset;
    document.querySelector("#bg2hofs-td").innerHTML = device.ppu.backgrounds[2].xOffset;
    document.querySelector("#bg2vofs-td").innerHTML = device.ppu.backgrounds[2].yOffset;
    document.querySelector("#bg3hofs-td").innerHTML = device.ppu.backgrounds[3].xOffset;
    document.querySelector("#bg3vofs-td").innerHTML = device.ppu.backgrounds[3].yOffset;
    document.querySelector("#bg2pa-td").innerHTML = device.ppu.backgrounds[2].dx;
    document.querySelector("#bg2pb-td").innerHTML = device.ppu.backgrounds[2].dmx;
    document.querySelector("#bg2pc-td").innerHTML = device.ppu.backgrounds[2].dy;
    document.querySelector("#bg2pd-td").innerHTML = device.ppu.backgrounds[2].dmy;
    document.querySelector("#bg2x-td").innerHTML = device.ppu.backgrounds[2].RefPointX;
    document.querySelector("#bg2y-td").innerHTML = device.ppu.backgrounds[2].RefPointY;
    document.querySelector("#bg3pa-td").innerHTML = device.ppu.backgrounds[3].dx;
    document.querySelector("#bg3pb-td").innerHTML = device.ppu.backgrounds[3].dmx;
    document.querySelector("#bg3pc-td").innerHTML = device.ppu.backgrounds[3].dy;
    document.querySelector("#bg3pd-td").innerHTML = device.ppu.backgrounds[3].dmy;
    document.querySelector("#bg3x-td").innerHTML = device.ppu.backgrounds[3].RefPointX;
    document.querySelector("#bg3y-td").innerHTML = device.ppu.backgrounds[3].RefPointY;
    document.querySelector("#mosaic-td").innerHTML = JSON.stringify(device.ppu.mosaic);
    document.querySelector("#bldcnt-td").innerHTML = JSON.stringify(device.ppu.colorEffects);
});

// Start up the connection to the signalr backend
connection.start().catch(err => document.write(err));

// Note that this mapping must match the enum ordering in Key.cs
var keyMap = {
    "x": 0, // A
    "z": 1, // B
    "Backspace": 2, // Select
    "Enter": 3, // Start
    "ArrowRight": 4, // Right
    "ArrowLeft": 5, // Left
    "ArrowUp": 6, // Up
    "ArrowDown": 7, // Down
    "a": 8, // L
    "s": 9, // R
}

document.addEventListener("keyup", function (e) {
    if (keyMap[e.key]) {
        connection.invoke("KeyUp", keyMap[e.key]);
    }
});

document.addEventListener("keydown", function(e) {
    if (keyMap[e.key]) {
        connection.invoke("KeyDown", keyMap[e.key]);
    }
});

document.querySelector("#pause-button").addEventListener("click", function (e) {
    connection.invoke("Pause");
    document.querySelector("#resume-button").classList.remove("d-none");
    document.querySelector("#pause-button").classList.add("d-none");
});

document.querySelector("#resume-button").addEventListener("click", function (e) {
    connection.invoke("Resume");
    document.querySelector("#pause-button").classList.remove("d-none");
    document.querySelector("#resume-button").classList.add("d-none");
});

/**
 * Called when a rom is clicked, this will fire a POST to the backend server 
 * which will start running that rom.
 * 
 * @param {string} guid - The GUID of a ROM loaded from the server.
 */
function LoadRom(guid) {
    document.querySelector("#pause-button").classList.remove("disabled");
    document.querySelector("#stop-button").classList.remove("disabled");

    fetch("api/v1/rom/load?guid=" + guid, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: "",
    }).then((response) => {
        console.log(response);
    });
}