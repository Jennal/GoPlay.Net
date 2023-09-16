const test = {
    "Test": "OK"
}

if (typeof window !== 'undefined') {
    (window as any)['Test'] = test;
}

export default test;
