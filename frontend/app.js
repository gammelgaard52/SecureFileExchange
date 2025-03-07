// Dynamically set API base URL based on environment
const API_BASE_URL = window.location.hostname.includes("localhost")
  ? "http://localhost:5000/api"
  : window.location.origin + "/api"; // Automatically detect Azure URL

/** ✅ Generate an AES encryption key from the password */
async function deriveKeyFromPassword(password) {
  const encoder = new TextEncoder();
  const salt = encoder.encode("fixed_salt_value"); // Ensure same salt for encryption & decryption
  const iterations = 100000;
  const hash = "SHA-256";

  // Convert password to key material
  const keyMaterial = await crypto.subtle.importKey(
    "raw",
    encoder.encode(password),
    { name: "PBKDF2" },
    false,
    ["deriveKey"]
  );

  // Derive key using PBKDF2
  return crypto.subtle.deriveKey(
    {
      name: "PBKDF2",
      salt: salt,
      iterations: iterations,
      hash: hash,
    },
    keyMaterial,
    { name: "AES-CBC", length: 256 },
    true,
    ["encrypt", "decrypt"]
  );
}

/** ✅ Encrypt file before upload */
async function encryptFile(file, key) {
  const iv = crypto.getRandomValues(new Uint8Array(16)); // Generate 16-byte IV

  const fileBuffer = await file.arrayBuffer();
  const encryptedBuffer = await crypto.subtle.encrypt(
    {
      name: "AES-CBC",
      iv: iv,
    },
    key,
    fileBuffer
  );

  // Prepend IV to encrypted data
  const combinedBuffer = new Uint8Array(iv.length + encryptedBuffer.byteLength);
  combinedBuffer.set(iv, 0);
  combinedBuffer.set(new Uint8Array(encryptedBuffer), iv.length);

  return { encryptedFile: new Blob([combinedBuffer]), iv };
}

async function uploadFile() {
  let fileInput = document.getElementById("fileInput");
  let passwordInput = document.getElementById("passwordInput");

  if (fileInput.files.length === 0 || passwordInput.value.trim() === "") {
    alert("Please select a file and enter a password.");
    return;
  }

  let file = fileInput.files[0];
  let key = await deriveKeyFromPassword(passwordInput.value); // Derive key once

  try {
    let { encryptedFile, iv } = await encryptFile(file, key); // Use the same key
    let formData = new FormData();
    formData.append("file", encryptedFile, file.name);
    formData.append("password", passwordInput.value);
    formData.append("iv", btoa(String.fromCharCode(...iv))); // Encode IV as Base64

    let response = await fetch(`${API_BASE_URL}/files/upload`, {
      method: "POST",
      body: formData,
    });

    if (!response.ok) {
      alert("File upload failed. Check console for details.");
      return;
    }

    alert("File uploaded successfully.");
  } catch (error) {
    alert("An error occurred while uploading the file.");
  }
}

async function downloadFile(password, fileId, fileName) {
  try {
    let response = await fetch(
      `${API_BASE_URL}/files/download/${password}/${fileId}`
    );
    let encryptedFile = await response.blob();
    let fileBuffer = await encryptedFile.arrayBuffer();

    // Extract IV (first 16 bytes) and encrypted data
    let iv = new Uint8Array(fileBuffer.slice(0, 16));
    let encryptedData = fileBuffer.slice(16);

    // Derive the same key from the password
    let key = await deriveKeyFromPassword(password);

    // Decrypt data
    let decryptedBuffer = await crypto.subtle.decrypt(
      { name: "AES-CBC", iv: iv },
      key,
      encryptedData
    );

    let decryptedFile = new Blob([decryptedBuffer], {
      type: "application/octet-stream",
    });
    let a = document.createElement("a");
    a.href = URL.createObjectURL(decryptedFile);
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  } catch (error) {
    alert("Failed to decrypt file. Ensure you entered the correct password.");
  }
}

async function listFiles() {
  let password = document.getElementById("listPasswordInput").value;
  if (!password) {
    alert("Please enter a password to list files.");
    return;
  }

  try {
    let response = await fetch(
      `${API_BASE_URL}/files/list/${encodeURIComponent(password)}`,
      {
        method: "GET",
      }
    );

    if (!response.ok) {
      console.error("Error listing files:", response.statusText);
      alert("Failed to retrieve file list. Check console for details.");
      return;
    }

    let files = await response.json();
    let fileList = document.getElementById("fileList");
    fileList.innerHTML = "";

    files.forEach((file) => {
      let listItem = document.createElement("li");
      listItem.innerHTML = `${file.fileName} 
                <button onclick="downloadFile('${password}', '${file.fileId}', '${file.fileName}')">Download</button>`;
      fileList.appendChild(listItem);
    });
  } catch (error) {
    console.error("Error:", error);
    alert("An error occurred while listing files.");
  }
}
