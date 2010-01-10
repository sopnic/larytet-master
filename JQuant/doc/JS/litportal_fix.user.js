// ==UserScript==
// @name     Litportal fix
// @description Fixes litportal.ru
// @include  http://www.litportal.ru/genre*/author*/read/*
// ==/UserScript==


function EmptyFunction()
{
}

function fixWindowGetSelection()
{
	unsafeWindow.LockSel = EmptyFunction;
}

var waitForTextCount = 0;

function unprotect(text)
{
	pageHtml = text.innerHTML;
	
	pageHtml = pageHtml.replace(new RegExp('<span class=[\'"]?h[\'"]?>[^<]*</span>','gi'), '');
	pageHtml = pageHtml.replace(new RegExp('xmlns:xlink="http://www.w3.org/1999/xlink"','gi'), '');
	pageHtml = pageHtml.replace(new RegExp('xmlns:fb="http://www.gribuser.ru/xml/fictionbook/2.0"','gi'), '');
	pageHtml = pageHtml.replace(new RegExp('<a name="@number"></a>','gi'), '');
	pageHtml = pageHtml.replace(new RegExp('&nbsp;','gi'), ' ');
	pageHtml = pageHtml.replace(new RegExp('[ \t][ \t]+','gi'), ' ');
	pageHtml = pageHtml.replace(new RegExp('<(h[1-6]|div|p)','gi'), '\n<$1');
	pageHtml = pageHtml.replace(new RegExp('[ \t]*align="justify"','gi'), '');

//	GM_log("Text1="+text.innerHTML);
	
	return text;
}

unsafeWindow.waitForText = function() 
{
	var text = document.getElementById("page_text");
	if (text == null) {
		++waitForTextCount;
		if (waitForTextCount < 200) {
			window.setTimeout("waitForText()", 200);
		}
	}
	else {  // i got the text
		// I want to make some changes in the look&fill. 
		// Specifically I am going to make left panel smaller
		modifyPage(text);
	}
}

var Links = new Array();
function findLinks()
{
	// <a class="main" href="http://www.litportal.ru/genre16/author4033/read/page/10/book17736.html">10</a>	
	var allLinks = document.evaluate("//a[@href]", document, null,
			XPathResult.UNORDERED_NODE_SNAPSHOT_TYPE, null);
	regExp = new RegExp("litportal.{1}ru/genre[0-9]+/author[0-9]+/read/page/[0-9]+/book[0-9]+", "gi");
	for (var i = 0; i < allLinks.snapshotLength; i++) {
		var link = allLinks.snapshotItem(i);
		href = link.getAttribute("href");
		if (href.match(regExp)) {
			Links.push(link);
		}
	}
}

function cleanUp() {
	all = document.evaluate("//table", document, null,
			XPathResult.UNORDERED_NODE_SNAPSHOT_TYPE, null);
	for (var i = 0; i < all.snapshotLength; i++) {
		var e = all.snapshotItem(i);
		e.parentNode.removeChild(e);
	}
	document.body.style.background = "white";
	document.body.style.color = "black";
//	document.innerHtml = "";
}

function modifyPage(text)
{
	// i need only two things on the page - links to the pages and text itself
	findLinks();
	GM_log("Found "+Links.length+" links");

	// remove all elements from the page
	if (Links.length > 0) {
		cleanUp();
	}
	var count = 1;
	while (Links.length > 0)
	{
		var link = Links.pop();

//		GM_log("Link="+link+", href="+href);
		// document.addChild(link);
		count++;
	}
	text = unprotect(text);//	GM_log("Text2="+text.innerHTML);
//	document.body.parent.addChild(text);
}

// Minor fix first
fixWindowGetSelection();

// wait until IFrame loads the text
window.setTimeout("waitForText()", 200);


