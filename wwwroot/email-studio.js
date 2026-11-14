window.emailStudioStorage = (() => {
    function open() {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open("email-studio", 1);
            request.onupgradeneeded = () => request.result.createObjectStore("library");
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
            request.onblocked = () => reject(new Error("Close other Email Studio tabs and try again."));
        });
    }

    async function read() {
        const db = await open();
        try {
            return await new Promise((resolve, reject) => {
                const tx = db.transaction("library", "readonly");
                const request = tx.objectStore("library").get("templates");
                request.onsuccess = () => resolve(request.result ?? null);
                request.onerror = () => reject(request.error);
                tx.onabort = () => reject(tx.error);
            });
        } finally { db.close(); }
    }

    async function write(expected, value) {
        const db = await open();
        try {
            await new Promise((resolve, reject) => {
                const tx = db.transaction("library", "readwrite");
                const store = tx.objectStore("library");
                let conflict = false;
                const request = store.get("templates");
                request.onsuccess = () => {
                    if ((request.result ?? null) !== expected) {
                        conflict = true;
                        tx.abort();
                    } else {
                        store.put(value, "templates");
                    }
                };
                tx.oncomplete = () => resolve();
                tx.onabort = () => reject(conflict
                    ? new Error("The library changed in another tab. Please retry.")
                    : tx.error ?? new Error("Could not save the library."));
                tx.onerror = () => {};
            });
        } finally { db.close(); }
    }

    function download(name, contentType, content) {
        const url = URL.createObjectURL(new Blob([content], { type: contentType }));
        const link = document.createElement("a");
        link.href = url;
        link.download = name;
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }

    return { read, write, download };
})();
