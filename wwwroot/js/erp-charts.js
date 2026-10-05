/* ==========================================================================
   BakeryFlow ERP — Chart.js bridge
   --------------------------------------------------------------------------
   Every chart on the reports page is built from the same theme tokens the rest
   of the app uses (--color-primary, --color-info, …) so charts stay legible
   across the bakeryflow / artisan / apricot palettes, and re-colour themselves
   the moment the active theme changes.
   ========================================================================== */
(function (global) {
    'use strict';

    if (typeof global.Chart === 'undefined') {
        console.warn('[ErpCharts] Chart.js failed to load; the charts view will stay empty.');
        return;
    }

    const registry = new Map();
    const currency = new Intl.NumberFormat('en-NG', {
        style: 'currency',
        currency: 'NGN',
        maximumFractionDigits: 0
    });

    /* ---------------------------------------------------------------- utils */

    function token(name, fallback) {
        const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
        return value || fallback;
    }

    function palette() {
        return {
            primary: token('--color-primary', '#f8b07a'),
            secondary: token('--color-secondary', '#1c1917'),
            accent: token('--color-accent', '#fdd7b5'),
            info: token('--color-info', '#3b82f6'),
            success: token('--color-success', '#10b981'),
            warning: token('--color-warning', '#f59e0b'),
            error: token('--color-error', '#ef4444'),
            content: token('--color-base-content', '#1c1917'),
            line: token('--color-base-300', '#e8eaed'),
            surface: token('--color-base-100', '#ffffff')
        };
    }

    /* Hex themes are the common case; anything else (oklch, color-mix) is passed
       through untouched so the browser resolves it. */
    function alpha(color, value) {
        const hex = /^#([0-9a-f]{6})$/i.exec(color || '');
        if (!hex) return color;
        const int = parseInt(hex[1], 16);
        return 'rgba(' + ((int >> 16) & 255) + ',' + ((int >> 8) & 255) + ',' + (int & 255) + ',' + value + ')';
    }

    function number(value) {
        return new Intl.NumberFormat('en-NG', { maximumFractionDigits: 1 }).format(Number(value) || 0);
    }

    function money(value) {
        return currency.format(Number(value) || 0);
    }

    function baseOptions(overrides) {
        const p = palette();
        return Object.assign({
            responsive: true,
            maintainAspectRatio: false,
            animation: { duration: 350 },
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        color: p.content,
                        boxWidth: 10,
                        boxHeight: 10,
                        usePointStyle: true,
                        pointStyle: 'circle',
                        font: { size: 10, weight: '600' },
                        padding: 14
                    }
                },
                tooltip: {
                    backgroundColor: p.secondary,
                    titleColor: p.surface,
                    bodyColor: p.surface,
                    borderColor: alpha(p.surface, 0.15),
                    borderWidth: 1,
                    padding: 10,
                    cornerRadius: 10,
                    titleFont: { size: 11, weight: '700' },
                    bodyFont: { size: 11 }
                }
            }
        }, overrides || {});
    }

    function axis(p, extra) {
        return Object.assign({
            border: { display: false },
            grid: { color: alpha(p.line, 0.9), drawBorder: false },
            ticks: { color: alpha(p.content, 0.65), font: { size: 10, weight: '600' } }
        }, extra || {});
    }

    /* -------------------------------------------------------------- charts */

    const LIFECYCLE_COLOURS = {
        requested: 'info',
        loaded: 'secondary',
        sold: 'success',
        returned: 'warning',
        damaged: 'error'
    };

    function lifecycleBar(payload) {
        const p = palette();
        const series = (payload && payload.series) || [];

        return {
            type: 'bar',
            data: {
                labels: (payload && payload.categories) || [],
                datasets: series.map(function (s) {
                    const key = LIFECYCLE_COLOURS[s.key] || 'primary';
                    return {
                        label: s.label,
                        data: s.data || [],
                        backgroundColor: p[key],
                        hoverBackgroundColor: p[key],
                        borderRadius: 4,
                        borderSkipped: false,
                        maxBarThickness: 28
                    };
                })
            },
            options: baseOptions({
                scales: {
                    x: axis(p, { stacked: false, grid: { display: false } }),
                    y: axis(p, { beginAtZero: true, ticks: { color: alpha(p.content, 0.65), font: { size: 10 }, callback: number } })
                }
            })
        };
    }

    function revenueLine(payload) {
        const p = palette();
        const points = (payload && payload.points) || [];

        return {
            type: 'line',
            data: {
                labels: points.map(function (x) { return x.label; }),
                datasets: [
                    {
                        label: 'Expected revenue',
                        data: points.map(function (x) { return x.value; }),
                        borderColor: p.primary,
                        backgroundColor: alpha(p.primary, 0.18),
                        pointBackgroundColor: p.primary,
                        pointBorderColor: p.surface,
                        pointBorderWidth: 1.5,
                        pointRadius: points.length > 40 ? 0 : 3,
                        pointHoverRadius: 5,
                        borderWidth: 2,
                        fill: true,
                        tension: 0.32
                    },
                    {
                        label: 'Cash / transfer collected',
                        data: points.map(function (x) { return x.secondaryValue; }),
                        borderColor: p.success,
                        backgroundColor: alpha(p.success, 0.12),
                        pointBackgroundColor: p.success,
                        pointBorderColor: p.surface,
                        pointBorderWidth: 1.5,
                        pointRadius: points.length > 40 ? 0 : 3,
                        pointHoverRadius: 5,
                        borderWidth: 2,
                        fill: true,
                        tension: 0.32
                    }
                ]
            },
            options: baseOptions({
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { color: p.content, usePointStyle: true, pointStyle: 'circle', boxWidth: 10, font: { size: 10, weight: '600' }, padding: 14 }
                    },
                    tooltip: {
                        backgroundColor: p.secondary,
                        titleColor: p.surface,
                        bodyColor: p.surface,
                        padding: 10,
                        cornerRadius: 10,
                        callbacks: { label: function (ctx) { return ' ' + ctx.dataset.label + ': ' + money(ctx.parsed.y); } }
                    }
                },
                scales: {
                    x: axis(p, { grid: { display: false } }),
                    y: axis(p, {
                        beginAtZero: true,
                        ticks: { color: alpha(p.content, 0.65), font: { size: 10 }, callback: function (v) { return money(v); } }
                    })
                }
            })
        };
    }

    function volumeDonut(payload) {
        const p = palette();
        const slices = (payload && payload.slices) || [];
        const ring = [p.primary, p.info, p.success, p.warning, p.accent, p.secondary];

        return {
            type: 'doughnut',
            data: {
                labels: slices.map(function (s) { return s.label; }),
                datasets: [{
                    data: slices.map(function (s) { return s.value; }),
                    backgroundColor: slices.map(function (_s, i) { return ring[i % ring.length]; }),
                    borderColor: p.surface,
                    borderWidth: 2,
                    hoverOffset: 8
                }]
            },
            options: baseOptions({
                cutout: '62%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { color: p.content, usePointStyle: true, pointStyle: 'circle', boxWidth: 10, font: { size: 10, weight: '600' }, padding: 12 }
                    },
                    tooltip: {
                        backgroundColor: p.secondary,
                        titleColor: p.surface,
                        bodyColor: p.surface,
                        padding: 10,
                        cornerRadius: 10,
                        callbacks: {
                            label: function (ctx) {
                                const total = ctx.dataset.data.reduce(function (a, b) { return a + b; }, 0);
                                const share = total ? ((ctx.parsed / total) * 100).toFixed(1) : '0.0';
                                return ' ' + ctx.label + ': ' + number(ctx.parsed) + ' units (' + share + '%)';
                            }
                        }
                    }
                }
            })
        };
    }

    function repPerformance(payload) {
        const p = palette();
        const rows = (payload && payload.rows) || [];

        return {
            type: 'bar',
            data: {
                labels: rows.map(function (r) { return r.repName; }),
                datasets: [
                    {
                        label: 'Units sold',
                        data: rows.map(function (r) { return r.soldUnits; }),
                        xAxisID: 'x',
                        backgroundColor: p.primary,
                        borderRadius: 4,
                        borderSkipped: false,
                        maxBarThickness: 16
                    },
                    {
                        label: 'Cash balance variance',
                        data: rows.map(function (r) { return r.varianceAmount; }),
                        xAxisID: 'x2',
                        backgroundColor: rows.map(function (r) { return Number(r.varianceAmount) >= 0 ? p.error : p.success; }),
                        borderRadius: 4,
                        borderSkipped: false,
                        maxBarThickness: 16
                    }
                ]
            },
            options: baseOptions({
                indexAxis: 'y',
                interaction: { mode: 'nearest', intersect: true },
                plugins: {
                    tooltip: {
                        backgroundColor: p.secondary,
                        titleColor: p.surface,
                        bodyColor: p.surface,
                        padding: 10,
                        cornerRadius: 10,
                        callbacks: {
                            afterBody: function (items) {
                                var row = rows[items[0].dataIndex];
                                if (!row) return '';
                                return [
                                    'Route: ' + row.route,
                                    'Expected: ' + money(row.expectedRevenue),
                                    'Outstanding: ' + money(row.outstandingBalance),
                                    'Variance is cash held versus the 32% plan.'
                                ];
                            }
                        }
                    }
                },
                scales: {
                    x: axis(p, { position: 'bottom', beginAtZero: true, title: { display: true, text: 'Units sold', color: alpha(p.content, 0.6), font: { size: 10, weight: '700' } } }),
                    x2: axis(p, { position: 'top', grid: { drawOnChartArea: false }, title: { display: true, text: 'Cash variance (₦)', color: alpha(p.content, 0.6), font: { size: 10, weight: '700' } } }),
                    y: axis(p, { grid: { display: false }, ticks: { color: p.content, font: { size: 10, weight: '700' } } })
                }
            })
        };
    }

    function logisticsStack(payload) {
        const p = palette();
        const rows = (payload && payload.rows) || [];

        return {
            type: 'bar',
            data: {
                labels: rows.map(function (r) { return r.vehicleRegistration; }),
                datasets: [
                    {
                        label: 'Fuel cost',
                        data: rows.map(function (r) { return r.fuelCost; }),
                        backgroundColor: p.warning,
                        stack: 'cost',
                        borderRadius: 3,
                        maxBarThickness: 48
                    },
                    {
                        label: 'Maintenance',
                        data: rows.map(function (r) { return r.maintenanceCost; }),
                        backgroundColor: p.error,
                        stack: 'cost',
                        borderRadius: 3,
                        maxBarThickness: 48
                    },
                    {
                        label: 'Trip expenses',
                        data: rows.map(function (r) { return r.tripExpenses; }),
                        backgroundColor: p.info,
                        stack: 'cost',
                        borderRadius: 3,
                        maxBarThickness: 48
                    },
                    {
                        type: 'line',
                        label: 'Delivery efficiency (units / 100 km)',
                        data: rows.map(function (r) { return r.deliveryEfficiencyIndex; }),
                        yAxisID: 'y2',
                        borderColor: p.success,
                        backgroundColor: p.success,
                        pointBackgroundColor: p.success,
                        borderWidth: 2,
                        tension: 0.3
                    }
                ]
            },
            options: baseOptions({
                plugins: {
                    tooltip: {
                        backgroundColor: p.secondary,
                        titleColor: p.surface,
                        bodyColor: p.surface,
                        padding: 10,
                        cornerRadius: 10,
                        callbacks: {
                            label: function (ctx) {
                                var isMoney = ctx.dataset.yAxisID !== 'y2';
                                return ' ' + ctx.dataset.label + ': ' + (isMoney ? money(ctx.parsed.y) : number(ctx.parsed.y));
                            },
                            afterBody: function (items) {
                                var row = rows[items[0].dataIndex];
                                if (!row) return '';
                                return row.trips + ' trip(s) • ' + number(row.distanceKm) + ' km • ' +
                                       number(row.litres) + ' L • on-time ' + row.onTimePercent + '%';
                            }
                        }
                    }
                },
                scales: {
                    x: axis(p, { stacked: true, grid: { display: false } }),
                    y: axis(p, { stacked: true, beginAtZero: true, ticks: { color: alpha(p.content, 0.65), font: { size: 10 }, callback: money } }),
                    y2: axis(p, { position: 'right', beginAtZero: true, grid: { drawOnChartArea: false } })
                }
            })
        };
    }

    /* ------------------------------------------------------------- renderer */

    function render(canvasId, builder, payload) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        const spec = builder(payload);
        if (!spec) return;

        destroy(canvasId);
        registry.set(canvasId, {
            chart: new global.Chart(canvas.getContext('2d'), spec),
            builder: builder,
            payload: payload
        });
    }

    function destroy(canvasId) {
        const entry = registry.get(canvasId);
        if (entry && entry.chart) entry.chart.destroy();
        registry.delete(canvasId);
    }

    function destroyAll() {
        registry.forEach(function (_entry, key) { destroy(key); });
    }

    function rebuildAll() {
        const p = palette();
        global.Chart.defaults.color = p.content;
        global.Chart.defaults.borderColor = p.line;

        registry.forEach(function (entry, key) {
            destroy(key);
            registry.set(key, {
                chart: new global.Chart(document.getElementById(key).getContext('2d'), entry.builder(entry.payload)),
                builder: entry.builder,
                payload: entry.payload
            });
        });
    }

    /* Charts follow the theme switcher, which writes data-theme on <html>. */
    if (typeof global.MutationObserver === 'function') {
        new global.MutationObserver(rebuildAll).observe(document.documentElement, {
            attributes: true,
            attributeFilter: ['data-theme']
        });
    }

    const p = palette();
    global.Chart.defaults.font.family = "'Plus Jakarta Sans', system-ui, sans-serif";
    global.Chart.defaults.font.size = 11;
    global.Chart.defaults.color = p.content;
    global.Chart.defaults.borderColor = p.line;

    global.ErpCharts = {
        render: render,
        destroy: destroy,
        destroyAll: destroyAll,
        palette: palette,
        money: money,
        number: number,
        builders: {
            lifecycleBar: lifecycleBar,
            revenueLine: revenueLine,
            volumeDonut: volumeDonut,
            repPerformance: repPerformance,
            logisticsStack: logisticsStack
        }
    };
})(window);
