"use strict";

function closeDialog() {
    window.parent.postMessage("CloseCustomActionDialogNoRefresh", "*");
}