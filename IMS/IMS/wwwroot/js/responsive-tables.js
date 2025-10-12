// Global Mobile Table Utility
class ResponsiveTable {
    constructor() {
        this.init();
    }

    init() {
        this.setupMobileTables();
        this.bindEvents();
    }

    setupMobileTables() {
        // Auto-initialize tables with specific classes
        $('table.mobile-responsive, .mobile-responsive-table').each((index, table) => {
            this.initializeTable($(table));
        });
    }

    initializeTable($table) {
        if (window.innerWidth < 768) {
            this.setMobileDataLabels($table);
        } else {
            this.removeMobileDataLabels($table);
        }
    }

    setMobileDataLabels($table) {
        const $headers = $table.find('thead th');

        $table.find('tbody tr').each((rowIndex, row) => {
            const $cells = $(row).find('td');

            $cells.each((cellIndex, cell) => {
                const $cell = $(cell);
                const headerText = $headers.eq(cellIndex).text().trim();

                if (headerText && !$cell.hasClass('no-label')) {
                    $cell.attr('data-label', headerText);
                }
            });
        });
    }

    removeMobileDataLabels($table) {
        $table.find('td[data-label]').removeAttr('data-label');
    }

    refreshTable($table) {
        this.initializeTable($table);
    }

    refreshAllTables() {
        $('table.mobile-responsive, .mobile-responsive-table').each((index, table) => {
            this.initializeTable($(table));
        });
    }

    bindEvents() {
        // Refresh on window resize
        $(window).on('resize', () => {
            setTimeout(() => this.refreshAllTables(), 100);
        });

        // Refresh when new rows are added (for dynamic tables)
        $(document).on('DOMNodeInserted', 'table.mobile-responsive tbody tr, .mobile-responsive-table tbody tr', (e) => {
            const $table = $(e.target).closest('table');
            setTimeout(() => this.refreshTable($table), 50);
        });
    }
}

// Initialize when document is ready
$(document).ready(() => {
    window.responsiveTable = new ResponsiveTable();
});

// Utility functions for dynamic tables
const TableUtils = {
    // Add row to any table
    addTableRow(tableSelector, rowHtml) {
        const $table = $(tableSelector);
        const $tbody = $table.find('tbody');

        // Remove empty state if exists
        $tbody.find('.empty-state').remove();

        $tbody.append(rowHtml);
        window.responsiveTable.refreshTable($table);
    },

    // Remove row from any table
    removeTableRow($row) {
        const $table = $row.closest('table');
        $row.remove();

        // Add empty state if no rows left
        if ($table.find('tbody tr').length === 0) {
            $table.find('tbody').append(`
                <tr class="empty-state">
                    <td colspan="${$table.find('thead th').length}" class="text-center py-5 text-muted">
                        <i class="bi bi-inbox display-4 d-block mb-2"></i>
                        <p class="mb-0">No data available</p>
                    </td>
                </tr>
            `);
        }

        window.responsiveTable.refreshTable($table);
    },

    // Reindex serial numbers
    reindexTableRows(tableSelector) {
        const $table = $(tableSelector);
        $table.find('tbody tr').each((index, row) => {
            $(row).find('td.serial-number').text(index + 1);
        });
        window.responsiveTable.refreshTable($table);
    }
};