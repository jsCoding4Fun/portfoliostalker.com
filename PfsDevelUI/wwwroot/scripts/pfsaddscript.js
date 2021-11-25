
function pfsAddScript(content) {

    var scriptElm = document.createElement('script');
    scriptElm.setAttribute('class', 'pfsScript');
    var inlineCode = document.createTextNode(content);
    scriptElm.appendChild(inlineCode);
    document.body.appendChild(scriptElm);
}
