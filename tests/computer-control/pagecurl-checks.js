// Load in a computer-control runtime providing cua, then call
// await runPageCurlChecks(cua, absoluteAppPath). Every action resolves from a fresh AX tree.
async function runPageCurlChecks(cua, appPath, scenes) {
    if (!appPath) throw new Error('Pass the absolute path to the built demo app.');
    const app = await cua.getApp(appPath);
    const results = [];
    const state = () => app.getAXState({ emit: false, disableDiffing: true });
    const index = (tree, description, role) => {
        const line = tree.split('\n').find(line =>
            line.includes(role) && line.includes(`Description: ${description},`));
        if (!line) throw new Error(`Missing ${role}: ${description}`);
        return Number(line.trim().match(/^\d+/)[0]);
    };
    const settled = async () => {
        for (let retry = 0; retry < 20; retry++) {
            const tree = await state();
            const buttons = tree.split('\n').filter(line => /Description: (Previous|Next),/.test(line));
            if (!tree.includes('Description: Turning,') &&
                buttons.some(line => !line.includes('(disabled)'))) return tree;
        }
        throw new Error('Navigation did not become ready within 20 observations.');
    };
    const click = async description => {
        const tree = await settled();
        if (!tree.includes(`Description: ${description},`)) description = description.replace(/^\d+\s+/, '');
        const line = tree.split('\n').find(line => line.includes(`Description: ${description},`));
        if (line && line.includes('(disabled)')) return tree;
        await app.click(index(tree, description, 'button'));
        return settled();
    };
    const expectCounter = (tree, counter, step) => {
        if (!tree.includes(`Description: ${counter},`))
            throw new Error(`${step}: expected ${counter}\n${tree}`);
        results.push(`PASS R04/R05/R08/R10 ${step}: ${counter}`);
    };
    const motionState = async () => {
        let tree = await state();
        if (!tree.includes('Description: Reduce page-turn motion,')) {
            await app.click(index(tree, 'Options', 'button'));
            tree = await state();
        }
        return tree;
    };
    const closeOptions = async tree => {
        if (tree.includes('Description: Close,')) {
            await app.click(index(tree, 'Close', 'button'));
            return state();
        }
        return tree;
    };
    let tree = await motionState();
    const wasReduced = tree.split('\n').find(line => line.includes('Description: Reduce page-turn motion,')).includes('Value: 1');
    try {
        if (!wasReduced) {
            await app.click(index(tree, 'Reduce page-turn motion', 'switch'));
            tree = await state();
        }
        tree = await closeOptions(tree);
        for (const [button, spread] of (scenes || [['01  Single', false], ['02  Spread', true],
            ['03  Content', false], ['04  Backside', false]])) {
            tree = await click(button);
            // Reach the first page even when the app already had navigation state.
            for (let turn = 0; turn < (spread ? 3 : 7); turn++) tree = await click('Previous');
            expectCounter(tree, spread ? '01–02 / 08' : '01 / 08', `${button} first boundary`);
            tree = await click('Next');
            expectCounter(tree, spread ? '03–04 / 08' : '02 / 08', `${button} forward`);
            tree = await click('Previous');
            expectCounter(tree, spread ? '01–02 / 08' : '01 / 08', `${button} backward`);
            for (let turn = 0; turn < (spread ? 3 : 7); turn++) tree = await click('Next');
            expectCounter(tree, spread ? '07–08 / 08' : '08 / 08', `${button} last boundary`);
            tree = await click('Next');
            expectCounter(tree, spread ? '07–08 / 08' : '08 / 08', `${button} rejects extra forward`);
            for (let turn = 0; turn < (spread ? 3 : 7); turn++) tree = await click('Previous');
            expectCounter(tree, spread ? '01–02 / 08' : '01 / 08', `${button} returns to first`);
            tree = await click('Previous');
            expectCounter(tree, spread ? '01–02 / 08' : '01 / 08', `${button} rejects extra backward`);
        }
    } finally {
        tree = await motionState();
        const motion = tree.split('\n').find(line => line.includes('Description: Reduce page-turn motion,'));
        if (motion.includes('Value: 1') !== wasReduced) {
            await app.click(index(tree, 'Reduce page-turn motion', 'switch'));
            tree = await state();
        }
        await closeOptions(tree);
    }
    return results;
}

// Choose coordinates from a fresh screenshot of the visible page, not a saved
// window size. Start from a different counter or status to detect a missed drag.
async function runPageCurlGestureCheck(cua, appPath, from, to, counter, status) {
    const app = await cua.getApp(appPath);
    const observe = () => app.getAXState({ emit: false, disableDiffing: true });
    const matches = tree => tree.includes(`Description: ${counter},`)
        && tree.includes(`Description: ${status},`);
    if (matches(await observe())) throw new Error('Gesture result already present before input.');
    await app.drag(from, to);
    for (let retry = 0; retry < 20; retry++) {
        if (matches(await observe())) return `PASS R03/R04/R08 pointer gesture: ${counter}, ${status}`;
    }
    throw new Error(`Gesture did not reach ${counter}, ${status}.`);
}

async function runPageCurlLiveContentCheck(cua, appPath) {
    const app = await cua.getApp(appPath);
    const observe = () => app.getAXState({ emit: false, disableDiffing: true });
    const click = async description => {
        const tree = await observe();
        const line = tree.split('\n').find(line => line.includes('button')
            && !line.includes('(disabled)') && line.includes(`Description: ${description},`));
        if (!line) throw new Error(`Missing enabled button: ${description}`);
        await app.click(Number(line.trim().match(/^\d+/)[0]));
        return observe();
    };
    const options = async () => {
        if (!(await observe()).includes('Description: View,')) await click('Options');
    };
    const close = async () => {
        if ((await observe()).includes('Description: Close,')) await click('Close');
    };
    const waitFor = async predicate => {
        for (let retry = 0; retry < 20; retry++) {
            const tree = await observe();
            if (predicate(tree)) return tree;
        }
        throw new Error('Content operation did not complete within 20 observations.');
    };
    const tree = await observe();
    await click(tree.includes('Description: 03  Content,') ? '03  Content' : 'Content');
    await options();
    await click('View');
    await close();
    const before = await waitFor(tree => tree.includes('Description: Increment the live content counter,'));
    const count = Number(before.match(/text Description: (\d+),/)[1]);
    await click('Increment the live content counter');
    await waitFor(tree => tree.includes(`Description: ${count + 1},`)
        && tree.includes('Description: Live content updated,'));
    await options();
    await click('Refresh snapshot');
    await waitFor(tree => tree.includes('Description: Snapshot refreshed,'));
    await close();
    return [`PASS R07/R10 live counter ${count} -> ${count + 1}, Live content updated`,
        'PASS R07/R10 explicit refresh: Snapshot refreshed'];
}
