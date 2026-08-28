// Copyright © Erickson Lopez. MIT License.
const assert = require('assert');
const {
  loadThresholds,
  parseScoreFromDescription,
  evaluateScore,
  verifyMutationGate,
  MAX_REPORT_AGE_DAYS
} = require('./verify-mutation-gate');

console.log('Running tests for verify-mutation-gate.js...\n');

// Test 1: loadThresholds from stryker-config.json
{
  const thresholds = loadThresholds();
  assert.strictEqual(thresholds.high, 100, 'Threshold high should be 100');
  assert.strictEqual(thresholds.low, 98, 'Threshold low should be 98');
  assert.strictEqual(thresholds.break, 95, 'Threshold break should be 95');
  console.log('✅ Test 1 Passed: loadThresholds loads correct values from stryker-config.json');
}

// Test 2: parseScoreFromDescription
{
  assert.strictEqual(parseScoreFromDescription('Stryker: 100% (240/240 killed) - ✅ HIGH'), 100);
  assert.strictEqual(parseScoreFromDescription('Stryker: 98.5% (200/203 killed) - 🟡 LOW'), 98.5);
  assert.strictEqual(parseScoreFromDescription('Stryker: 95.0% - 🟠 WARNING'), 95.0);
  assert.strictEqual(parseScoreFromDescription('Stryker: 94.2% - ❌ FAILED'), 94.2);
  assert.strictEqual(parseScoreFromDescription(null), null);
  assert.strictEqual(parseScoreFromDescription('No percentage here'), null);
  console.log('✅ Test 2 Passed: parseScoreFromDescription correctly extracts numeric percentage');
}

// Test 3: evaluateScore
{
  const thresholds = { high: 100, low: 98, break: 95 };

  const resHigh = evaluateScore(100, thresholds);
  assert.strictEqual(resHigh.status, '✅ HIGH');
  assert.strictEqual(resHigh.passedBreak, true);

  const resLow = evaluateScore(98.5, thresholds);
  assert.strictEqual(resLow.status, '🟡 LOW');
  assert.strictEqual(resLow.passedBreak, true);

  const resWarn = evaluateScore(96.0, thresholds);
  assert.strictEqual(resWarn.status, '🟠 WARNING');
  assert.strictEqual(resWarn.passedBreak, true);

  const resBreakExact = evaluateScore(95.0, thresholds);
  assert.strictEqual(resBreakExact.status, '🟠 WARNING');
  assert.strictEqual(resBreakExact.passedBreak, true);

  const resFail = evaluateScore(94.9, thresholds);
  assert.strictEqual(resFail.status, '❌ FAILED');
  assert.strictEqual(resFail.passedBreak, false);

  console.log('✅ Test 3 Passed: evaluateScore correctly categorizes scores and break gate');
}

// Test 4: verifyMutationGate with mock direct target SHA (100% HIGH)
(async () => {
  let failed = false;
  const mockContext = {
    repo: { owner: 'ericksonlopezf', repo: 'dotnet-specification' },
    sha: 'abc1234567890'
  };

  const freshDate = new Date().toISOString();

  const mockGithub = {
    rest: {
      repos: {
        getCombinedStatusForRef: async ({ ref }) => {
          if (ref === 'abc1234567890') {
            return {
              data: {
                statuses: [
                  {
                    context: 'mutation-testing/stryker',
                    state: 'success',
                    description: 'Stryker: 100% (240/240 killed) - ✅ HIGH',
                    updated_at: freshDate,
                    target_url: 'https://github.com/ericksonlopezf/dotnet-specification/actions/runs/12345'
                  }
                ]
              }
            };
          }
          return { data: { statuses: [] } };
        }
      }
    }
  };

  const mockCore = {
    setFailed: () => { failed = true; },
    setOutput: () => {},
    summary: { addRaw: () => ({ write: async () => {} }) }
  };

  await verifyMutationGate({ github: mockGithub, context: mockContext, core: mockCore });
  assert.strictEqual(failed, false, 'Should pass for 100% score on target commit');
  console.log('✅ Test 4 Passed: verifyMutationGate succeeds with direct 100% commit status');
})();

// Test 5: verifyMutationGate with score below break threshold (<95% FAILED)
(async () => {
  let failed = false;
  const mockContext = {
    repo: { owner: 'ericksonlopezf', repo: 'dotnet-specification' },
    sha: 'fail1234567890'
  };

  const freshDate = new Date().toISOString();

  const mockGithub = {
    rest: {
      repos: {
        getCombinedStatusForRef: async () => {
          return {
            data: {
              statuses: [
                {
                  context: 'mutation-testing/stryker',
                  state: 'failure',
                  description: 'Stryker: 80.0% (160/200 killed) - ❌ FAILED',
                  updated_at: freshDate,
                  target_url: 'https://github.com/ericksonlopezf/dotnet-specification/actions/runs/12346'
                }
              ]
            }
          };
        }
      }
    }
  };

  const mockCore = {
    setFailed: () => { failed = true; },
    setOutput: () => {},
    summary: { addRaw: () => ({ write: async () => {} }) }
  };

  try {
    await verifyMutationGate({ github: mockGithub, context: mockContext, core: mockCore });
    assert.fail('Should have thrown an error for score below break threshold');
  } catch (err) {
    assert.strictEqual(failed, true, 'core.setFailed should be called');
    console.log('✅ Test 5 Passed: verifyMutationGate blocks release for sub-break score');
  }
})();

// Test 6: verifyMutationGate with WARNING score (96% >= 95% break threshold -> ALLOWED)
(async () => {
  let failed = false;
  const mockContext = {
    repo: { owner: 'ericksonlopezf', repo: 'dotnet-specification' },
    sha: 'warn1234567890'
  };

  const freshDate = new Date().toISOString();

  const mockGithub = {
    rest: {
      repos: {
        getCombinedStatusForRef: async ({ ref }) => {
          if (ref === 'warn1234567890') {
            return {
              data: {
                statuses: [
                  {
                    context: 'mutation-testing/stryker',
                    state: 'success',
                    description: 'Stryker: 96.0% (192/200 killed) - 🟠 WARNING',
                    updated_at: freshDate,
                    target_url: 'https://github.com/ericksonlopezf/dotnet-specification/actions/runs/12347'
                  }
                ]
              }
            };
          }
          return { data: { statuses: [] } };
        }
      }
    }
  };

  const mockCore = {
    setFailed: () => { failed = true; },
    setOutput: () => {},
    summary: { addRaw: () => ({ write: async () => {} }) }
  };

  await verifyMutationGate({ github: mockGithub, context: mockContext, core: mockCore });
  assert.strictEqual(failed, false, 'Should allow release for 96% score (warning, but >= 95% break)');
  console.log('✅ Test 6 Passed: verifyMutationGate permits release for WARNING score >= 95%');
})();

// Test 7: verifyMutationGate with expired report (>7 days) -> BLOCKED in enforce mode
(async () => {
  let failed = false;
  const mockContext = {
    repo: { owner: 'ericksonlopezf', repo: 'dotnet-specification' },
    sha: 'expired1234567'
  };

  const eightDaysAgo = new Date(Date.now() - 8 * 24 * 60 * 60 * 1000).toISOString();

  const mockGithub = {
    rest: {
      repos: {
        getCombinedStatusForRef: async () => {
          return {
            data: {
              statuses: [
                {
                  context: 'mutation-testing/stryker',
                  state: 'success',
                  description: 'Stryker: 100% (240/240 killed) - ✅ HIGH',
                  updated_at: eightDaysAgo,
                  target_url: 'https://github.com/ericksonlopezf/dotnet-specification/actions/runs/12340'
                }
              ]
            }
          };
        }
      }
    }
  };

  const mockCore = {
    setFailed: () => { failed = true; },
    setOutput: () => {},
    summary: { addRaw: () => ({ write: async () => {} }) }
  };

  try {
    await verifyMutationGate({ github: mockGithub, context: mockContext, core: mockCore });
    assert.fail('Should have failed for expired report');
  } catch (err) {
    assert.strictEqual(failed, true, 'core.setFailed should be called for expired report');
    console.log('✅ Test 7 Passed: verifyMutationGate blocks release when report is expired (> 7 days)');
  }
})();

// Test 8: verifyMutationGate with production code drift in src/ -> BLOCKED in enforce mode
(async () => {
  let failed = false;
  const mockContext = {
    repo: { owner: 'ericksonlopezf', repo: 'dotnet-specification' },
    sha: 'targetShaWithDrift'
  };

  const freshDate = new Date().toISOString();

  const mockGithub = {
    rest: {
      repos: {
        getCombinedStatusForRef: async ({ ref }) => {
          if (ref === 'targetShaWithDrift') {
            return { data: { statuses: [] } };
          }
          if (ref === 'evaluatedOlderSha') {
            return {
              data: {
                statuses: [
                  {
                    context: 'mutation-testing/stryker',
                    state: 'success',
                    description: 'Stryker: 100% (240/240 killed) - ✅ HIGH',
                    updated_at: freshDate,
                    target_url: 'https://github.com/ericksonlopezf/dotnet-specification/actions/runs/12348'
                  }
                ]
              }
            };
          }
          return { data: { statuses: [] } };
        },
        listCommits: async () => ({
          data: [
            { sha: 'targetShaWithDrift' },
            { sha: 'evaluatedOlderSha' }
          ]
        }),
        compareCommits: async ({ base, head }) => ({
          data: {
            files: [
              { filename: 'src/EricksonLopez.Specification/Spec.cs', status: 'modified' }
            ]
          }
        })
      }
    }
  };

  const mockCore = {
    setFailed: () => { failed = true; },
    setOutput: () => {},
    summary: { addRaw: () => ({ write: async () => {} }) }
  };

  try {
    await verifyMutationGate({ github: mockGithub, context: mockContext, core: mockCore });
    assert.fail('Should have failed for src/ code drift');
  } catch (err) {
    assert.strictEqual(failed, true, 'core.setFailed should be called for src/ drift');
    console.log('✅ Test 8 Passed: verifyMutationGate blocks release when src/ files were modified after evaluation');
  }
})();

// Test 9: verifyMutationGate with non-production changes (docs/version bump) -> ALLOWED
(async () => {
  let failed = false;
  const mockContext = {
    repo: { owner: 'ericksonlopezf', repo: 'dotnet-specification' },
    sha: 'releaseTagSha'
  };

  const freshDate = new Date().toISOString();

  const mockGithub = {
    rest: {
      repos: {
        getCombinedStatusForRef: async ({ ref }) => {
          if (ref === 'releaseTagSha') {
            return { data: { statuses: [] } };
          }
          if (ref === 'testedMainSha') {
            return {
              data: {
                statuses: [
                  {
                    context: 'mutation-testing/stryker',
                    state: 'success',
                    description: 'Stryker: 100% (240/240 killed) - ✅ HIGH',
                    updated_at: freshDate,
                    target_url: 'https://github.com/ericksonlopezf/dotnet-specification/actions/runs/12349'
                  }
                ]
              }
            };
          }
          return { data: { statuses: [] } };
        },
        listCommits: async () => ({
          data: [
            { sha: 'releaseTagSha' },
            { sha: 'testedMainSha' }
          ]
        }),
        compareCommits: async ({ base, head }) => ({
          data: {
            files: [
              { filename: 'CHANGELOG.md', status: 'modified' },
              { filename: 'Directory.Build.props', status: 'modified' },
              { filename: '.release-please-manifest.json', status: 'modified' }
            ]
          }
        })
      }
    }
  };

  const mockCore = {
    setFailed: () => { failed = true; },
    setOutput: () => {},
    summary: { addRaw: () => ({ write: async () => {} }) }
  };

  await verifyMutationGate({ github: mockGithub, context: mockContext, core: mockCore });
  assert.strictEqual(failed, false, 'Should allow release when only docs/metadata files changed');
  console.log('✅ Test 9 Passed: verifyMutationGate permits release when changes are limited to docs/versioning (zero src/ drift)');
})();

// Test 10: evaluate mode with NO prior evidence -> needs_stryker: true, does not fail
(async () => {
  let failed = false;
  let outputs = {};
  const mockContext = {
    repo: { owner: 'ericksonlopezf', repo: 'dotnet-specification' },
    sha: 'newShaNoEvidence'
  };

  const mockGithub = {
    rest: {
      repos: {
        getCombinedStatusForRef: async () => ({ data: { statuses: [] } }),
        listCommits: async () => ({ data: [] })
      },
      actions: {
        listWorkflowRuns: async () => ({ data: { workflow_runs: [] } })
      }
    }
  };

  const mockCore = {
    setFailed: () => { failed = true; },
    setOutput: (k, v) => { outputs[k] = v; },
    summary: { addRaw: () => ({ write: async () => {} }) }
  };

  const res = await verifyMutationGate({ github: mockGithub, context: mockContext, core: mockCore, mode: 'evaluate' });
  assert.strictEqual(failed, false, 'Should not fail in evaluate mode when no evidence exists');
  assert.strictEqual(res.needs_stryker, true, 'needs_stryker should be true');
  assert.strictEqual(res.reason, 'no_evidence', 'reason should be no_evidence');
  assert.strictEqual(outputs.needs_stryker, 'true', 'core output needs_stryker should be "true"');
  console.log('✅ Test 10 Passed: evaluate mode with NO prior evidence sets needs_stryker=true without failing');
})();

// Test 11: evaluate mode with expired report (>7 days) -> needs_stryker: true, does not fail
(async () => {
  let failed = false;
  let outputs = {};
  const mockContext = {
    repo: { owner: 'ericksonlopezf', repo: 'dotnet-specification' },
    sha: 'expiredInEval'
  };

  const eightDaysAgo = new Date(Date.now() - 8 * 24 * 60 * 60 * 1000).toISOString();

  const mockGithub = {
    rest: {
      repos: {
        getCombinedStatusForRef: async () => ({
          data: {
            statuses: [
              {
                context: 'mutation-testing/stryker',
                state: 'success',
                description: 'Stryker: 100% (240/240 killed) - ✅ HIGH',
                updated_at: eightDaysAgo,
                target_url: 'https://github.com/ericksonlopezf/dotnet-specification/actions/runs/12340'
              }
            ]
          }
        })
      }
    }
  };

  const mockCore = {
    setFailed: () => { failed = true; },
    setOutput: (k, v) => { outputs[k] = v; },
    summary: { addRaw: () => ({ write: async () => {} }) }
  };

  const res = await verifyMutationGate({ github: mockGithub, context: mockContext, core: mockCore, mode: 'evaluate' });
  assert.strictEqual(failed, false, 'Should not fail in evaluate mode when report is expired');
  assert.strictEqual(res.needs_stryker, true, 'needs_stryker should be true');
  assert.strictEqual(res.reason, 'expired', 'reason should be expired');
  assert.strictEqual(outputs.needs_stryker, 'true', 'core output needs_stryker should be "true"');
  console.log('✅ Test 11 Passed: evaluate mode with expired report sets needs_stryker=true without failing');
})();

// Test 12: evaluate mode with src/ drift -> needs_stryker: true, does not fail
(async () => {
  let failed = false;
  let outputs = {};
  const mockContext = {
    repo: { owner: 'ericksonlopezf', repo: 'dotnet-specification' },
    sha: 'driftInEval'
  };

  const freshDate = new Date().toISOString();

  const mockGithub = {
    rest: {
      repos: {
        getCombinedStatusForRef: async ({ ref }) => {
          if (ref === 'driftInEval') return { data: { statuses: [] } };
          if (ref === 'olderShaInEval') {
            return {
              data: {
                statuses: [
                  {
                    context: 'mutation-testing/stryker',
                    state: 'success',
                    description: 'Stryker: 100% (240/240 killed) - ✅ HIGH',
                    updated_at: freshDate,
                    target_url: 'https://github.com/ericksonlopezf/dotnet-specification/actions/runs/12348'
                  }
                ]
              }
            };
          }
          return { data: { statuses: [] } };
        },
        listCommits: async () => ({
          data: [
            { sha: 'driftInEval' },
            { sha: 'olderShaInEval' }
          ]
        }),
        compareCommits: async () => ({
          data: {
            files: [
              { filename: 'src/EricksonLopez.Specification/Spec.cs', status: 'modified' }
            ]
          }
        })
      }
    }
  };

  const mockCore = {
    setFailed: () => { failed = true; },
    setOutput: (k, v) => { outputs[k] = v; },
    summary: { addRaw: () => ({ write: async () => {} }) }
  };

  const res = await verifyMutationGate({ github: mockGithub, context: mockContext, core: mockCore, mode: 'evaluate' });
  assert.strictEqual(failed, false, 'Should not fail in evaluate mode when src drift exists');
  assert.strictEqual(res.needs_stryker, true, 'needs_stryker should be true');
  assert.strictEqual(res.reason, 'src_drift', 'reason should be src_drift');
  assert.strictEqual(outputs.needs_stryker, 'true', 'core output needs_stryker should be "true"');
  console.log('✅ Test 12 Passed: evaluate mode with src/ drift sets needs_stryker=true without failing');
})();

// Test 13: evaluate mode with fresh valid report -> needs_stryker: false, can_proceed: true
(async () => {
  let failed = false;
  let outputs = {};
  const mockContext = {
    repo: { owner: 'ericksonlopezf', repo: 'dotnet-specification' },
    sha: 'freshInEval'
  };

  const freshDate = new Date().toISOString();

  const mockGithub = {
    rest: {
      repos: {
        getCombinedStatusForRef: async ({ ref }) => {
          if (ref === 'freshInEval') {
            return {
              data: {
                statuses: [
                  {
                    context: 'mutation-testing/stryker',
                    state: 'success',
                    description: 'Stryker: 100% (240/240 killed) - ✅ HIGH',
                    updated_at: freshDate,
                    target_url: 'https://github.com/ericksonlopezf/dotnet-specification/actions/runs/12349'
                  }
                ]
              }
            };
          }
          return { data: { statuses: [] } };
        }
      }
    }
  };

  const mockCore = {
    setFailed: () => { failed = true; },
    setOutput: (k, v) => { outputs[k] = v; },
    summary: { addRaw: () => ({ write: async () => {} }) }
  };

  const res = await verifyMutationGate({ github: mockGithub, context: mockContext, core: mockCore, mode: 'evaluate' });
  assert.strictEqual(failed, false, 'Should not fail in evaluate mode when valid report exists');
  assert.strictEqual(res.needs_stryker, false, 'needs_stryker should be false');
  assert.strictEqual(res.can_proceed, true, 'can_proceed should be true');
  assert.strictEqual(outputs.needs_stryker, 'false', 'core output needs_stryker should be "false"');
  assert.strictEqual(outputs.can_proceed, 'true', 'core output can_proceed should be "true"');
  console.log('✅ Test 13 Passed: evaluate mode with fresh valid report sets needs_stryker=false, can_proceed=true');
})();
