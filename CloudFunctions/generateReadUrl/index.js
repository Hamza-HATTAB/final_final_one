const functions = require('firebase-functions');
const { Storage } = require('@google-cloud/storage');
const admin = require('firebase-admin');
const cors = require('cors')({ origin: true });

// Initialize Firebase Admin SDK if not already initialized
try {
  admin.initializeApp();
} catch (e) {
  console.log('Admin SDK already initialized');
}

// Create storage client
const storage = new Storage();

exports.generateReadUrl = functions.https.onRequest((request, response) => {
  return cors(request, response, async () => {
    try {
      // Check request method
      if (request.method !== 'POST') {
        return response.status(405).send({ error: 'Method not allowed. Please use POST.' });
      }

      // Get authorization header
      const authHeader = request.headers.authorization;
      if (!authHeader || !authHeader.startsWith('Bearer ')) {
        return response.status(401).send({ error: 'Unauthorized. Missing or invalid authorization header.' });
      }

      // Extract token
      const idToken = authHeader.split('Bearer ')[1];
      
      try {
        // Verify Firebase ID token
        const decodedToken = await admin.auth().verifyIdToken(idToken);
        console.log('User authenticated:', decodedToken.uid);
        
        // Extract request parameters
        const { objectName, bucketName } = request.body;
        
        if (!objectName || !bucketName) {
          return response.status(400).send({ error: 'Missing required parameters: objectName and bucketName are required.' });
        }
        
        console.log(`Generating signed read URL for: bucket=${bucketName}, object=${objectName}`);
        
        // Check if the file exists
        const file = storage.bucket(bucketName).file(objectName);
        const [exists] = await file.exists();
        
        if (!exists) {
          console.error(`File ${objectName} does not exist in bucket ${bucketName}`);
          return response.status(404).send({ error: 'File not found in storage.' });
        }
        
        // Generate signed URL for reading
        const options = {
          version: 'v4',
          action: 'read',
          expires: Date.now() + 60 * 60 * 1000, // 1 hour
        };
        
        const [signedUrl] = await file.getSignedUrl(options);
        
        console.log('Generated signed read URL successfully');
        
        // Return the signed URL
        return response.status(200).send({ signedUrl });
      } catch (authError) {
        console.error('Error verifying token:', authError);
        return response.status(401).send({ error: 'Unauthorized. Invalid token.' });
      }
    } catch (error) {
      console.error('Error generating signed read URL:', error);
      return response.status(500).send({ error: `Internal server error: ${error.message}` });
    }
  });
}); 