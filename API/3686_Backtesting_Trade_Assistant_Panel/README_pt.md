# Estratégia do painel de assistentes comerciais de backtesting
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia do Backtesting Trade Assistant Panel** é um auxiliar manual convertido do MetaTrader 4 consultor especialista *Backtesting Trade Assistant Panel V1.10*. O script original criava um painel de controle gráfico dentro do testador que permitia ao operador alterar o tamanho do lote, as distâncias de stop-loss e take-profit e enviar instantaneamente ordens de mercado de COMPRA ou VENDA. A porta StockSharp oferece o mesmo fluxo de trabalho dentro de um componente de estratégia, expondo parâmetros fortemente tipados e métodos auxiliares públicos em vez de widgets no gráfico.

Principais capacidades:

- Mantenha o volume de pedidos configurável junto com distâncias de stop-loss e take-profit no estilo MetaTrader (medidas em “pontos”).
- Emita ordens de mercado longas ou curtas sob demanda por meio dos ajudantes `ManualBuy()` e `ManualSell()`.
- Anexe automaticamente compensações de stop-loss e take-profit após cada entrada manual usando os valores de pontos convertidos.
- Fornece métodos utilitários que atualizam o volume de negociação e as distâncias de risco em tempo de execução, imitando os campos de texto editáveis do painel MT4.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `OrderVolume` | Volume em lotes aplicado em ordens manuais de mercado. Alterar o valor também atualiza a base `Strategy.Volume`. | `0.1` |
| `StopLossPips` | Distância do preço de preenchimento até o stop de proteção, expressa em MetaTrader pontos. Defina como `0` para desativar a colocação automática de stop-loss. | `50` |
| `TakeProfitPips` | Distância do preço de preenchimento até a meta de lucro, expressa em MetaTrader pontos. Defina como `0` para desativar a colocação automática de lucro. | `100` |
| `MagicNumber` | Identificador preservado do original EA para escrituração contábil. Não é usado diretamente pela lógica de execução StockSharp, mas pode ser referenciado em extensões personalizadas. | `99` |

## Operações manuais
O EA original dependia de botões clicáveis. Em StockSharp as mesmas ações estão disponíveis como métodos públicos:

- `SetOrderVolume(decimal volume)` – sincroniza o parâmetro `OrderVolume` e o valor interno `Strategy.Volume`.
- `SetStopLoss(decimal pips)` / `SetTakeProfit(decimal pips)` – ajuste as distâncias de proteção enquanto a estratégia está em execução. Os valores são interpretados em MetaTrader pontos, exatamente como as caixas de texto MT4.
- `ManualBuy()` – envia uma ordem de compra a mercado usando o volume atual. Após a execução, a estratégia converte os pontos de stop-loss e take-profit configurados em compensações de preço (usando metadados de símbolos) e chama `SetStopLoss`/`SetTakeProfit` para registrar ordens de proteção para a posição líquida resultante.
- `ManualSell()` – auxiliar simétrico para ordens de venda a mercado.
- `CloseAllPositions()` – fecha toda a exposição ao preço de mercado. Isso reflete o fluxo de trabalho em que o testador pode nivelar as posições manualmente.

Todas as distâncias de proteção são convertidas com a mesma heurística de tamanho de pip do MT4: símbolos de cinco e três dígitos multiplicam `PriceStep` por dez para obter um único “ponto”, enquanto outros símbolos dependem do `PriceStep` bruto. Se os dados de mercado não fornecerem os metadados necessários, um tamanho substituto de `0.0001` será usado para preservar um comportamento consistente.

## Notas comportamentais
- A estratégia assina atualizações de Nível 1 para acompanhar o melhor lance/venda. Quando esses preços não estão disponíveis, ele volta ao último preço comercial antes de anexar compensações de proteção.
- Nenhum sinal de negociação automático é gerado – este módulo atua estritamente como um assistente de execução manual, assim como o painel MT4.
- Como StockSharp gerencia ordens de proteção nativamente, não há necessidade de um número mágico explícito. O campo é incluído apenas por questão de paridade com o consultor especialista de origem.
- As distâncias de stop-loss e take-profit podem ser ajustadas a qualquer momento antes de acionar `ManualBuy()`/`ManualSell()` para emular a edição dos campos de texto MT4 antes de pressionar os botões.

## Diferenças do original EA
- A interface do usuário MetaTrader é substituída por parâmetros de estratégia e chamadas de método. Todas as funcionalidades estão disponíveis programaticamente sem renderizar controles de gráfico.
- O tratamento de deslizamento da chamada MT4 `OrderSend` (fixado em 50 pontos) não é reproduzido porque os ajudantes `BuyMarket`/`SellMarket` de StockSharp não expõem um argumento de deslizamento direto. O ambiente circundante deve gerenciar a tolerância de execução, se necessário.
- As ordens de proteção são criadas com ajudantes `SetStopLoss`/`SetTakeProfit` de alto nível de StockSharp em vez de chamadas `OrderSend` diretas, mantendo a implementação consistente com as convenções de StockSharp.

## Dicas de uso
1. Configure o símbolo, portfólio e conector desejados em StockSharp como de costume e, em seguida, inicie a estratégia.
2. Ajuste `OrderVolume`, `StopLossPips` e `TakeProfitPips` por meio da grade de parâmetros ou dos métodos setter fornecidos.
3. Ligue para `ManualBuy()` ou `ManualSell()` sempre que uma entrada discricionária for necessária. O ajudante anexará automaticamente as ordens de proteção solicitadas.
4. Use `CloseAllPositions()` para nivelar a exposição instantaneamente durante backtests ou sessões de negociação discricionárias ao vivo.
