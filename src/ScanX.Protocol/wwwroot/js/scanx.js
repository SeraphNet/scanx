﻿
$(function () {
    
})


class ScanX {

    constructor() {

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:61234/scanx")
            .configureLogging(signalR.LogLevel.Information)
            .build();
    }
    
    connect() {

        this.connection.start().catch(err => console.error(err.toString()));
    }

    scan() {

        this.connection.invoke("Scan").catch(err => console.error(err.toString()));
    }
}