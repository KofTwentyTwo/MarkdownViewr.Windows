// Copyright (c) 2026 James Maes (KofTwentyTwo)
// SPDX-License-Identifier: MIT

"use strict";

/** Requires genuine passing CI on the exact complete tree merged for this release. */
async function run({ github, context, core })
{
   const repository = context.repo;
   const { data: source } = await github.rest.repos.getCommit({ ...repository, ref: context.sha });
   if(!source.commit.verification.verified)
   {
      throw new Error("The release commit signature is not verified.");
   }
   const pulls = await github.paginate(github.rest.repos.listPullRequestsAssociatedWithCommit,
      { ...repository, commit_sha: context.sha, per_page: 100 });
   const merged = pulls.filter(pull => pull.merged_at && pull.state === "closed"
      && pull.base.ref === "main" && pull.merge_commit_sha === context.sha);
   if(merged.length !== 1)
   {
      throw new Error("Expected one merged main PR for the tagged release commit.");
   }
   const pull = merged[0];
   const { data: tested } = await github.rest.repos.getCommit({ ...repository, ref: pull.head.sha });
   if(tested.commit.tree.sha !== source.commit.tree.sha)
   {
      throw new Error("The release tree differs from the tested PR tree.");
   }
   const { data: rules } = await github.request("GET /repos/{owner}/{repo}/rules/branches/{branch}",
      { ...repository, branch: "main" });
   // Personal acknowledgement is enforced at merge; technical validation is reused here.
   const required = rules.filter(rule => rule.type === "required_status_checks")
      .flatMap(rule => rule.parameters.required_status_checks)
      .filter(check => check.context !== "review / automated");
   if(required.length === 0)
   {
      throw new Error("No required technical checks were found.");
   }
   const checks = await github.paginate(github.rest.checks.listForRef,
      { ...repository, ref: pull.head.sha, per_page: 100 });
   for(const requirement of required)
   {
      const records = checks.filter(check => check.name === requirement.context
         && check.app?.id === (requirement.integration_id ?? 15368));
      records.sort((left, right) => right.id - left.id);
      if(records[0]?.status !== "completed" || records[0]?.conclusion !== "success")
      {
         throw new Error(`Required tested-source check is not successful: ${requirement.context}`);
      }
   }
   core.info(`Reused ${required.length} genuine passing checks from PR #${pull.number} at ${pull.head.sha}; exact release tree ${source.commit.tree.sha}.`);
   return { head: pull.head.sha, tree: source.commit.tree.sha, checks: required.length };
}

module.exports = { run };
