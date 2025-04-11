import {convert} from 'openapi-to-postmanv2';

// Replace with your values
const POSTMAN_API_KEY = 'YOUR_POSTMAN_API_KEY';
const COLLECTION_UID = 'YOUR_COLLECTION_UID';
const OPENAPI_JSON_URL = 'https://example.com/your-collection.json'; // Publicly accessible JSON URL

async function fetchOpenApiSpec(url) {
    const res = await fetch(url);
    if (!res.ok) throw new Error(`Failed to fetch OpenAPI spec: ${res.statusText}`);
    return await res.json();
}

async function convertOpenApiToPostman(openapiJson) {
    return new Promise((resolve, reject) => {
        convert({type: 'json', data: openapiJson}, {}, (err, result) => {
            if (err || !result.result) {
                reject(err || result.reason);
            } else {
                resolve(result.output[0].data);
            }
        });
    });
}

async function updatePostmanCollection(postmanCollection) {
    const payload = JSON.stringify({collection: postmanCollection});

    const res = await fetch(`https://api.getpostman.com/collections/${COLLECTION_UID}`, {
        method: 'PUT',
        headers: {
            'X-Api-Key': POSTMAN_API_KEY,
            'Content-Type': 'application/json',
        },
        body: payload,
    });

    const result = await res.json();
    if (!res.ok) {
        throw new Error(`Postman API error: ${JSON.stringify(result)}`);
    }

    console.log(`✅ Updated Postman collection: ${result.collection.name}`);
}

// Make sure you're running this with Node.js v18 or later
async function main() {
    process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
    try {
        const openapiJson = await fetchOpenApiSpec(OPENAPI_JSON_URL);
        const postmanCollection = await convertOpenApiToPostman(openapiJson);
        await updatePostmanCollection(postmanCollection);
    } catch (err) {
        console.error(err)
        console.error('❌ Error:', err.message);
    }
}

main().then(() => console.log('✅ Done'));