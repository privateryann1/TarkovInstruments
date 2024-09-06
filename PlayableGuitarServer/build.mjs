import { execSync } from 'child_process';
import { fileURLToPath } from 'url';
import path from 'path';

// Convert the current module file URL to a path
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Run the TypeScript compiler
try {
    console.log('Transpiling TypeScript to JavaScript...');
    execSync('tsc', { stdio: 'inherit' });
    console.log('Build completed successfully!');
} catch (error) {
    console.error('Error during build:', error);
    process.exit(1);
}