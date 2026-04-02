<template>
  <div>
    <div class="breadcrumb">
      <a href="#">Docs</a> <span class="sep">›</span> <span>Alliances</span>
    </div>

    <h1>ALLIANCES <span class="tag">SOCIAL</span></h1>
    <p class="page-lead">
      Alliances allow groups of players to coordinate, share intelligence, and support each other
      in the Wasteland. Joining or founding an alliance unlocks diplomacy, a private forum,
      and internal messaging.
    </p>

    <h2 id="create">Creating an Alliance</h2>
    <p>
      Any player can create an alliance. Once created, the founder becomes the
      <strong class="hl">Leader</strong>. Other players can submit join applications
      which the Leader and Officers can approve or reject.
    </p>

    <table class="stat-table">
      <thead>
        <tr><th>Aspect</th><th>Detail</th></tr>
      </thead>
      <tbody>
        <tr><td class="val">Name</td><td>Unique name chosen at creation</td></tr>
        <tr><td class="val">Tag</td><td>Short abbreviation shown next to usernames</td></tr>
        <tr><td class="val">Requirement</td><td>CouncilHall unlocks advanced alliance options</td></tr>
        <tr><td class="val">Membership</td><td>Apply to join; leader/officers approve</td></tr>
        <tr><td class="val">Leaving</td><td>Members can leave at any time; leaders must transfer first</td></tr>
      </tbody>
    </table>

    <h2 id="ranks">Member Ranks</h2>
    <p>
      Every alliance member has a rank that determines their permissions within the alliance:
    </p>

    <table class="stat-table">
      <thead>
        <tr>
          <th>Rank</th>
          <th>Permissions</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td class="val" style="color: var(--amber);">👑 Leader</td>
          <td>Full control — manage members, set diplomacy, disband alliance, post forum</td>
        </tr>
        <tr>
          <td class="val" style="color: var(--cyan);">⭐ Officer</td>
          <td>Approve/reject applications, manage lower-rank members, post forum</td>
        </tr>
        <tr>
          <td class="val" style="color: var(--text);">🔰 Veteran</td>
          <td>Trusted member — can post in alliance forum, view member list</td>
        </tr>
        <tr>
          <td class="val" style="color: var(--muted);">👤 Member</td>
          <td>Basic access — can read forum, view alliance info, receive alliance messages</td>
        </tr>
      </tbody>
    </table>

    <h2 id="diplomacy">Diplomacy States</h2>
    <p>
      Alliances can set formal diplomatic relations with other alliances. These relations
      affect how players interact on the map:
    </p>

    <table class="stat-table">
      <thead>
        <tr>
          <th>State</th>
          <th>Effect</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td><span style="color: var(--green); font-weight: 600;">Allied</span></td>
          <td>No-aggression pact. Raiding allied settlement members is strongly discouraged and visible on the map.</td>
        </tr>
        <tr>
          <td><span style="color: var(--text); font-weight: 600;">Neutral</span></td>
          <td>Default state. No special rules — normal player interaction applies.</td>
        </tr>
        <tr>
          <td><span style="color: var(--amber); font-weight: 600;">Hostile</span></td>
          <td>Open warfare declared. All members of the hostile alliance are valid raid targets with no diplomatic consequences.</td>
        </tr>
      </tbody>
    </table>

    <div class="info-box warning">
      <div class="info-label">⚠ DIPLOMACY IS PUBLIC</div>
      Diplomatic states between alliances are visible to all players. Declaring hostility
      alerts the target alliance and may trigger retaliation.
    </div>

    <h2 id="forum">Alliance Forum</h2>
    <p>
      Each alliance has a private forum for internal coordination. Forum posts are organized
      into topics. Officers and above can create new topics; members and above can reply.
    </p>

    <table class="stat-table">
      <thead>
        <tr><th>Feature</th><th>Access</th></tr>
      </thead>
      <tbody>
        <tr><td class="val">Create topics</td><td>Officer, Veteran, Leader</td></tr>
        <tr><td class="val">Reply to topics</td><td>All members</td></tr>
        <tr><td class="val">See forum</td><td>All members</td></tr>
        <tr><td class="val">Delete posts</td><td>Leader, Officer</td></tr>
      </tbody>
    </table>

    <div class="info-box">
      <div class="info-label">☢ MESSAGING</div>
      You can send direct messages to any player through the Messages view. Alliance members
      can also be messaged via the Alliance member list. All messages are private and only
      visible to sender and recipient.
    </div>

    <div class="page-nav">
      <router-link to="/wasteland">
        <span class="nav-hint">← Previous</span>
        <span class="nav-label">Wasteland</span>
      </router-link>
      <router-link to="/introduction" style="text-align:right">
        <span class="nav-hint">↑ Back to top</span>
        <span class="nav-label">Introduction</span>
      </router-link>
    </div>
  </div>
</template>
