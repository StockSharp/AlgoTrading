# Estratégia Hans123 Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Hans123 Trader é um sistema de rompimento convertido do assessor especialista original do MetaTrader 5 *Hans123_Trader*. A estratégia verifica um intervalo de preços dinâmico e coloca ordens pendentes de stop durante uma janela intradiária configurável. Stops de proteção, metas de lucro e regras de trailing espelham a lógica MQL5 para que o port StockSharp se comporte como o robô fonte.

## Conceitos principais
- **Rompimento de intervalo** – usa a máxima mais alta e a mínima mais baixa das últimas *N* velas para definir o canal de rompimento.
- **Filtro de tempo** – avalia sinais apenas entre as horas de início e fim para evitar ruído noturno.
- **Ordens pendentes síncronas** – atualiza ordens buy stop e sell stop a cada vela completa dentro da janela de negociação.
- **Controle de risco** – distâncias opcionais de stop-loss, take-profit e trailing stop expressas em pips.
- **Trailing dinâmico** – assim que o preço percorre a distância de trailing stop mais trailing step, o stop de proteção é ajustado para garantir ganhos.

## Lógica de negociação
1. Subscrever a série de velas selecionada e aguardar a formação da janela do indicador `RangeLength`.
2. A cada vela finalizada:
   - Atualizar o canal de máxima/mínima de 80 barras (configurável).
   - Pular o processamento se o horário atual estiver fora do intervalo `[StartHour, EndHour)`.
   - Cancelar ordens de entrada existentes e colocar novas ordens stop:
     - **Buy stop** na máxima do intervalo por `OrderVolume`.
     - **Sell stop** na mínima do intervalo por `OrderVolume`.
3. Quando uma ordem de entrada é executada:
   - Cancelar a ordem pendente oposta.
   - Registrar ordens de stop-loss e take-profit se as distâncias em pips correspondentes forem maiores que zero.
4. Enquanto uma posição estiver aberta:
   - Se o preço avançar pelo menos `TrailingStopPips + TrailingStepPips`, mover o stop de proteção em direção ao mercado por `TrailingStopPips`.
   - As ordens de proteção são canceladas automaticamente quando a posição volta a zero.

## Parâmetros
| Nome | Descrição | Valor padrão |
| --- | --- | --- |
| `OrderVolume` | Tamanho da ordem para entradas em rompimento. | `0.1` |
| `RangeLength` | Número de velas no canal de rompimento. | `80` |
| `StopLossPips` | Distância do stop-loss em pips (0 desativa o stop). | `50` |
| `TakeProfitPips` | Distância do take-profit em pips (0 desativa o alvo). | `50` |
| `TrailingStopPips` | Distância do trailing stop em pips (0 desativa o trailing). | `10` |
| `TrailingStepPips` | Pips adicionais necessários antes de o trailing stop ser atualizado. Deve ser positivo quando o trailing estiver habilitado. | `5` |
| `StartHour` | Hora inclusiva do dia (UTC) em que as ordens de rompimento começam. | `6` |
| `EndHour` | Hora exclusiva do dia (UTC) em que as ordens de rompimento param. | `10` |
| `CandleType` | Tipo de dados de vela e período de trabalho. | Velas de `1 hora` |

## Notas práticas
- O tamanho do pip se adapta aos decimais do instrumento (símbolos forex de 3/5 dígitos recebem o ajuste usual *×10*).
- Os trailing stops só são criados após uma posição percorrer a distância de ativação; se `StopLossPips` for zero, o stop inicial é omitido até que as condições de trailing sejam atendidas.
- Manter as permissões do portfólio alinhadas com o `OrderVolume` selecionado e o tamanho do contrato do instrumento.
- A conversão StockSharp usa auxiliares de gráfico para visualizar velas, o canal e operações para depuração.

## Diferenças em relação à versão MQL5
- As ordens de stop e alvo são registradas através dos auxiliares de alto nível do StockSharp, em vez de solicitações de negociação do MetaTrader.
- Os valores padrão de volume permanecem idênticos (0.1 lotes), mas podem ser otimizados via metadados `StrategyParam`.
- As ordens pendentes são atualizadas a cada vela completa, em vez de aguardar atualizações no nível de tick, adaptando-se ao modelo de eventos do StockSharp.

## Uso
1. Anexar a estratégia a um par de portfólio/instrumento e verificar se a subscrição de velas corresponde ao período desejado.
2. Ajustar os parâmetros para a volatilidade do instrumento e os limites de sessão.
3. Iniciar a estratégia; monitorar a sobreposição da área do gráfico para confirmar os níveis de rompimento e as operações executadas.
4. Usar os parâmetros integrados para otimização dentro do ambiente de testes do StockSharp, se desejado.
