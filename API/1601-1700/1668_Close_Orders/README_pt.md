# Estratégia de Fechamento de Ordens
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utilitária fecha imediatamente posições existentes e cancela ordens pendentes de acordo com filtros definidos pelo usuário. Pode operar apenas no instrumento anexado ou em todos os instrumentos do portfólio. As restrições opcionais de janela de tempo e faixa de preço permitem controle preciso sobre quais ordens são afetadas.

## Detalhes

- **Propósito**: gerenciamento de risco e liquidação manual.
- **Operação**:
  - No início, a estratégia verifica a janela de tempo opcional.
  - Se permitido, fecha posições e cancela ordens que correspondam aos filtros.
  - Após o processamento, a estratégia para automaticamente.
- **Filtros**:
  - `CloseAllSecurities` – incluir todos os instrumentos do portfólio em vez de apenas o instrumento anexado.
  - `CloseOpenLongOrders` / `CloseOpenShortOrders` – fechar posições compradas ou vendidas existentes.
  - `ClosePendingLongOrders` / `ClosePendingShortOrders` – cancelar ordens de compra ou venda pendentes.
  - `SpecificOrderId` – apenas tocar ordens com o id de transação fornecido quando diferente de zero.
  - `CloseOrdersWithinRange`, `CloseRangeHigh`, `CloseRangeLow` – limitar por faixa de preço de entrada.
  - `EnableTimeControl`, `StartCloseTime`, `StopCloseTime` – aplicar apenas durante uma janela de tempo específica.
- **Valores padrão**:
  - Todas as opções de fechamento habilitadas.
  - `SpecificOrderId` = 0.
  - `CloseOrdersWithinRange` = false.
  - `CloseRangeHigh` = 0.
  - `CloseRangeLow` = 0.
  - `EnableTimeControl` = false.
  - `StartCloseTime` = 02:00.
  - `StopCloseTime` = 02:30.
- **Notas**:
  - A estratégia não abre novas posições.
  - Os filtros de faixa de preço são ignorados quando os limites são zero ou negativos.
  - Quando `CloseAllSecurities` está habilitado, as posições em todo o portfólio são processadas.
