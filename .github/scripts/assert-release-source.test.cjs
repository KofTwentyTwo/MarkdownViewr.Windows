// Copyright (c) 2026 James Maes (KofTwentyTwo)
// SPDX-License-Identifier: MIT

"use strict";

const assert = require("node:assert/strict");
const test = require("node:test");
const { run } = require("./assert-release-source.cjs");

/** Exercises release-tree, check-provider and latest-result boundaries. */
test("reuses only verified identical-source passing CI", async () =>
{
   for(const failure of [null, "signature", "tree", "merge", "provider", "pending", "failed", "missing", "latest", "rules"])
   {
      const check = { id: 1, name: "ci / ui-tests", app: { id: failure === "provider" ? 7 : 15368 },
         status: failure === "pending" ? "in_progress" : "completed",
         conclusion: failure === "failed" ? "failure" : "success" };
      const github = {
         rest: {
            repos: { getCommit: async ({ ref }) => ({ data: { commit: {
               verification: { verified: failure !== "signature" },
               tree: { sha: failure === "tree" && ref === "head" ? "other" : "tree" },
            } } }), listPullRequestsAssociatedWithCommit: "pulls" },
            checks: { listForRef: "checks" },
         },
         request: async () => ({ data: failure === "rules" ? [] : [{ type: "required_status_checks",
            parameters: { required_status_checks: [{ context: "ci / ui-tests", integration_id: 15368 }, { context: "review / automated" }] } }] }),
         paginate: async method => method === "pulls" ? (failure === "merge" ? [] : [{ number: 1, state: "closed",
            merged_at: "today", base: { ref: "main" }, merge_commit_sha: "release", head: { sha: "head" } }])
            : failure === "missing" ? [] : failure === "latest" ? [check, { ...check, id: 2, conclusion: "failure" }] : [check],
      };
      const operation = run({ github, context: { repo: { owner: "owner", repo: "repo" }, sha: "release" }, core: { info: () => {} } });
      if(failure) { await assert.rejects(operation); }
      else { assert.deepEqual(await operation, { head: "head", tree: "tree", checks: 1 }); }
   }
});
