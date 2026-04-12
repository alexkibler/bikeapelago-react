import { test, expect, request } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

test.describe('Single Player Route Setup API', () => {
    let token: string;
    let userId: string;
    const API_BASE_URL = process.env.API_BASE_URL || 'http://localhost:5054';
    const gpxFilePath = path.join(process.cwd(), '..', '..', '..', '..', 'COURSE_448681500.gpx');

    test.beforeAll(async () => {
        const apiContext = await request.newContext();

        // Login as testuser
        const loginResponse = await apiContext.post(`${API_BASE_URL}/api/auth/login`, {
            data: {
                identity: 'testuser',
                password: 'Password'
            }
        });

        expect(loginResponse.status()).toBe(200);
        
        const loginData = await loginResponse.json();
        token = loginData.token;
        userId = loginData.record.id;
    });

    test('should setup session from GPX file and verify nodes', async () => {
        const apiContext = await request.newContext();
        
        // 1. Setup session from route
        const fileBuffer = fs.readFileSync(gpxFilePath);
        
        const response = await apiContext.post(`${API_BASE_URL}/api/sessions/setup-from-route`, {
            headers: {
                'Authorization': `Bearer ${token}`
            },
            multipart: {
                file: {
                    name: 'COURSE_448681500.gpx',
                    mimeType: 'application/gpx+xml',
                    buffer: fileBuffer,
                },
                nodeCount: '5'
            }
        });

        expect(response.status()).toBe(200);
        
        const data = await response.json();
        expect(data.session).toBeDefined();
        expect(data.session.mode).toBe('singleplayer');
        expect(data.session.status).toBe('Active');
        expect(data.summary.nodeCount).toBe(5);
        
        const sessionId = data.session.id;

        // 2. Fetch nodes and verify initial state
        const nodesResponse = await apiContext.get(`${API_BASE_URL}/api/sessions/${sessionId}/nodes`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        expect(nodesResponse.status()).toBe(200);
        const nodes = await nodesResponse.json();
        expect(nodes.length).toBe(5);
        
        const availableNodes = nodes.filter((n: any) => n.state === 'Available');
        const hiddenNodes = nodes.filter((n: any) => n.state === 'Hidden');
        
        expect(availableNodes.length).toBe(3);
        expect(hiddenNodes.length).toBe(2);
        
        // Sort by apLocationId to test sequential unlock
        const sortedNodes = nodes.sort((a: any, b: any) => a.apLocationId - b.apLocationId);

        // 3. Test node unlock mechanism
        // We take the last "Available" node and check it
        const nodeToUnlock = sortedNodes[2]; // Index 2 is the 3rd node (last Available one)
        const nextHiddenNode = sortedNodes[3]; // Index 3 is the 4th node (first Hidden one)
        
        const patchResponse = await apiContext.patch(`${API_BASE_URL}/api/nodes/${nodeToUnlock.id}`, {
            headers: {
                'Authorization': `Bearer ${token}`
            },
            data: {
                state: 'Checked'
            }
        });
        
        expect(patchResponse.status()).toBe(200);
        
        // Fetch nodes again to verify the next one is unlocked
        const nodesAfterUpdateResponse = await apiContext.get(`${API_BASE_URL}/api/sessions/${sessionId}/nodes`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        const nodesAfterUpdate = await nodesAfterUpdateResponse.json();
        const updatedNode = nodesAfterUpdate.find((n: any) => n.id === nodeToUnlock.id);
        const newlyAvailableNode = nodesAfterUpdate.find((n: any) => n.id === nextHiddenNode.id);
        
        expect(updatedNode.state).toBe('Checked');
        expect(newlyAvailableNode.state).toBe('Available');
    });
});
