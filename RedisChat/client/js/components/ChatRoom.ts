import * as ReconnectingWebSocket from 'reconnecting-websocket';

class ChatRoomController implements angular.IController {

    $inject = ['$scope'];
    $scope: angular.IScope;

    messages: string[];
    socket: WebSocket;
    chat: string;

    constructor($scope) {
        this.$scope = $scope;
    }

    submit() {
        this.socket.send(this.chat);
        this.chat = null;
    }

    addNewMessage(s: string) {
        console.log(s);
        this.messages.push(s);

        // We need to do this manually because DOM changes happen outside the AngularJS world.
        this.$scope.$apply();
    }

    $onInit() {
        this.messages = [];

        this.socket = new ReconnectingWebSocket('ws://' + location.host);

        this.socket.onopen = e => {
            this.addNewMessage('Chat connection opened.');
        };

        this.socket.onclose = e => {
            this.addNewMessage('Chat connection closed.');
        };

        this.socket.onmessage = e => {
            this.addNewMessage(e.data);
        };
    }
}

export let ChatRoomComponent: angular.IComponentOptions = {
    controller: ChatRoomController,
    controllerAs: 'me',
    template: require('./ChatRoom.html')
}
