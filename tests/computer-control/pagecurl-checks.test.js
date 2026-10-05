const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const context = vm.createContext({});
vm.runInContext(fs.readFileSync(`${__dirname}/pagecurl-checks.js`, 'utf8'), context);

function fixture(brokenBoundary = false, initialMotion = false) {
    let page = 1;
    let reduced = initialMotion;
    const app = {
        async getAXState() {
            return [
                '0 button Description: 01  Single,',
                `1 button${page === 1 && brokenBoundary !== 'first' ? ' (disabled)' : ''} Description: Previous,`,
                `2 button${page === 8 && brokenBoundary !== 'last' ? ' (disabled)' : ''} Description: Next,`,
                `3 text Description: ${String(page).padStart(2, '0')} / 08,`,
                `4 switch Description: Reduce page-turn motion, Value: ${Number(reduced)},`,
            ].join('\n');
        },
        async click(index) {
            if (index === 1) page = Math.max(1, page - 1);
            if (index === 2) page = Math.min(8, page + 1);
            if (index === 4) reduced = !reduced;
        },
    };
    return { cua: { getApp: async () => app }, reduced: () => reduced };
}

test('navigation checks accept valid boundaries and restore motion', async () => {
    for (const initialMotion of [false, true]) {
        const fake = fixture(false, initialMotion);
        const results = await context.runPageCurlChecks(fake.cua, '/demo.app', [['01  Single', false]]);
        assert.equal(results.length, 7);
        assert.equal(fake.reduced(), initialMotion);
    }
});

test('unchanged counter cannot hide an enabled boundary button', async () => {
    const fake = fixture('first');
    await assert.rejects(context.runPageCurlChecks(fake.cua, '/demo.app', [['01  Single', false]]), /Previous.*disabled/);
    assert.equal(fake.reduced(), false);
});

test('last boundary must disable Next even when the counter stops advancing', async () => {
    const fake = fixture('last');
    await assert.rejects(context.runPageCurlChecks(fake.cua, '/demo.app', [['01  Single', false]]), /Next.*disabled/);
    assert.equal(fake.reduced(), false);
});

function accessibleFixture(wrongRole = false) {
    let open = false;
    const controls = [
        '0 button Description: Options,',
        '1 button Description: Previous,',
        '2 button Description: Next,',
        '3 button Description: Close,',
        '4 slider Description: Page change threshold,',
        '5 text field Description: Choose page width to height ratio,',
        '6 switch Description: Enable flick gestures,',
        '7 switch Description: Enable page-turn haptic feedback,',
        `8 ${wrongRole ? 'text' : 'switch'} Description: Reduce page-turn motion,`,
    ];
    const app = {
        async getAXState() { return (open ? controls : controls.slice(0, 3)).join('\n'); },
        async click(index) { if (index === 0) open = true; if (index === 3) open = false; },
    };
    return { cua: { getApp: async () => app }, open: () => open };
}

test('accessible controls are verified and compact options are closed', async () => {
    const fake = accessibleFixture();
    assert.equal((await context.runPageCurlAccessibilityCheck(fake.cua, '/demo.app')).length, 7);
    assert.equal(fake.open(), false);
});

test('a text label cannot stand in for an accessible switch', async () => {
    const fake = accessibleFixture(true);
    await assert.rejects(context.runPageCurlAccessibilityCheck(fake.cua, '/demo.app'), /incorrect role/);
    assert.equal(fake.open(), false);
});
