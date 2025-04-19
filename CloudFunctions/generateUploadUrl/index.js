const functions = require('firebase-functions');
const { Storage } = require('@google-cloud/storage');
const admin = require('firebase-admin');
const cors = require('cors')({ origin: true });

// Initialize Firebase Admin SDK with explicit project ID
// Make sure this matches the project where your tokens are issued
admin.initializeApp({
  projectId: 'thesis-manager-backend' // IMPORTANT: Replace with your actual Firebase project ID if different
});

// Create storage client
const storage = new Storage();

exports.generateUploadUrl = functions.https.onRequest((request, response) => {
  return cors(request, response, async () => {
    console.log('Received request for generateUploadUrl');

    try {
      // Check request method
      if (request.method !== 'POST') {
        return response.status(405).send({ error: 'Method not allowed. Please use POST.' });
      }

      // Get authorization header
      const authHeader = request.headers.authorization;
      if (!authHeader || !authHeader.startsWith('Bearer ')) {
        console.log('Missing or invalid authorization header');
        return response.status(401).send({ error: 'Unauthorized. Missing or invalid authorization header.' });
      }

      // Extract token
      const idToken = authHeader.split('Bearer ')[1];
      console.log('Received token starting with:', idToken.substring(0, 10) + '...');
      
      try {
        // Verify Firebase ID token
        console.log('Attempting to verify Firebase ID token...');
        const decodedToken = await admin.auth().verifyIdToken(idToken);
        console.log('Token successfully verified for user:', decodedToken.uid);
        console.log('Token claims:', JSON.stringify(decodedToken, null, 2));
        
        // Extract request parameters
        const { objectName, bucketName, contentType = 'application/octet-stream' } = request.body;
        console.log(`Request body: ${JSON.stringify(request.body)}`);
        
        if (!objectName || !bucketName) {
          console.log('Missing required parameters: objectName or bucketName');
          return response.status(400).send({ error: 'Missing required parameters: objectName and bucketName are required.' });
        }
        
        console.log(`Generating signed URL for: bucket=${bucketName}, object=${objectName}, contentType=${contentType}`);
        
        // Generate signed URL for uploading
        const options = {
          version: 'v4',
          action: 'write',
          expires: Date.now() + 15 * 60 * 1000, // 15 minutes
          contentType: contentType
        };
        
        console.log('Getting signed URL with options:', JSON.stringify(options));
        const [signedUrl] = await storage.bucket(bucketName).file(objectName).getSignedUrl(options);
        
        console.log('Generated signed URL successfully');
        
        // Return the signed URL
        return response.status(200).send({ signedUrl });
      } catch (authError) {
        console.error('Error verifying token:', authError);
        console.error('Token details for debugging:', idToken.substring(0, 20) + '...');
        return response.status(401).send({ 
          error: 'Unauthorized. Invalid token.',
          details: authError.message 
        });
      }
    } catch (error) {
      console.error('Error generating signed URL:', error);
      return response.status(500).send({ error: `Internal server error: ${error.message}` });
    }
  });
}); 