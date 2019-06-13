function _(data) {
    var context = getContext();
    var collection = context.getCollection();
    var response = context.getResponse();

    function throwIfError(err, resource) {
        if (err) throw "|{code: " + err.number + ", message: '" + err.message + "'}|";
    }

    function ensureAccepted(accepted, call) { if (!accepted) throwIfError({ number: 406, message: 'Call ' + call + ' not accepted!' }); }

    function upsertItem(item, continuation) {
        if (item._etag === "*") {
            ensureAccepted(collection.upsertDocument(collection.getSelfLink(), item, { disableAutomaticIdGeneration: true }, continuation), "upsert");
        } else if (item._etag) {
            var itemToReplaceLink = collection.getAltLink() + '/docs/' + item.id;
            ensureAccepted(collection.replaceDocument(itemToReplaceLink, item, { etag: item._etag }, continuation), "replace");
        } else {
            ensureAccepted(collection.createDocument(collection.getSelfLink(), item, { disableAutomaticIdGeneration: false }, continuation), "insert");
        }
    }

    function processOne(items, index) {
        if (index >= items.length) return;
        upsertItem(items[index], function(err, resource) {
            throwIfError(err, resource);
            processOne(items, index + 1);
        });
    }

    processOne(data, 0);
}
