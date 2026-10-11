import { readFileSync } from 'node:fs';

const spec = JSON.parse(readFileSync(new URL('../contracts/reports.openapi.json', import.meta.url)));
const requiredPaths = [
  '/api/workspaces/{workspaceId}/reports/daily',
  '/api/workspaces/{workspaceId}/reports/weekly',
  '/api/workspaces/{workspaceId}/reports/reorder-list',
  '/api/workspaces/{workspaceId}/reports/reorder-list/{productId}',
  '/api/workspaces/{workspaceId}/reports/alert-settings',
];
const missingPaths = requiredPaths.filter((path) => !spec.paths[path]);
if (missingPaths.length) {
  console.error(`Missing Reports paths: ${missingPaths.join(', ')}`);
  process.exit(1);
}

const requiredOperations = [
  'getDailyReport',
  'getWeeklyReport',
  'getReportsReorderList',
  'setReportsReorderListItem',
  'getReportAlertSettings',
  'updateReportAlertSettings',
];
const operationIds = Object.values(spec.paths).flatMap((path) => Object.values(path).map((operation) => operation.operationId));
const missingOperations = requiredOperations.filter((operation) => !operationIds.includes(operation));
if (missingOperations.length) {
  console.error(`Missing Reports operations: ${missingOperations.join(', ')}`);
  process.exit(1);
}

const client = readFileSync(new URL('../frontend/src/api/generated.ts', import.meta.url), 'utf8');
const clientOperations = ['dailyReport', 'weeklyReport', 'reportsReorderList', 'setReportsReorderListItem', 'reportAlertSettings', 'updateReportAlertSettings'];
const missingClientOperations = clientOperations.filter((operation) => !client.includes(`${operation}(`));
if (missingClientOperations.length) {
  console.error(`Missing Reports client operations: ${missingClientOperations.join(', ')}`);
  process.exit(1);
}

console.log('Reports OpenAPI document and generated client are aligned.');
