# Estratégia de Máquina Comercial AIS4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **AIS4 Trade Machine Strategy** é um assistente de negociação manual que transporta o consultor especialista original MetaTrader "AIS4 Trade Machine" para StockSharp. Ele mantém o fluxo de trabalho de uma posição do script: o operador fornece níveis absolutos de stop-loss e take-profit, emite um comando e a estratégia calcula o tamanho da negociação com base no patrimônio da conta corrente e nas especificações do instrumento. Após o preenchimento da ordem de mercado, a estratégia envia imediatamente ordens de proteção emparelhadas (stop + limite) para que os níveis de risco e recompensa solicitados sejam aplicados no lado da bolsa.

A estratégia **não** gera sinais automáticos. Ele foi projetado para execução discricionária onde o usuário decide quando e onde inserir ou modificar uma posição.

## Fluxo de trabalho manual
1. Certifique-se de que o instrumento conectado exponha `PriceStep`, `StepPrice`, `VolumeStep`, `MinVolume` e `MaxVolume`. Eles são obrigados a converter o risco de preço em tamanho do contrato e a alinhar o volume de pedidos com os limites cambiais.
2. Antes de enviar um comando, defina `StopPrice` e `TakePrice` para os níveis de preço absolutos que deseja usar.
3. Altere `Command` para `Buy` ou `Sell`. A estratégia:
   - Verifica se nenhuma outra posição está aberta.
   - Verifica se o stop-loss e o take-profit solicitados respeitam a distância mínima do tick.
   - Calcula o orçamento de risco a partir de `OrderReserve` × patrimônio líquido atual do portfólio e garante que a reserva de patrimônio (`AccountReserve`) seja respeitada.
   - Estima o volume do pedido a partir da distância de parada e do valor do tick do instrumento.
   - Envia a ordem de mercado e, em seguida, envia ordens de proteção emparelhadas (`SellStop`+`SellLimit` para posições compradas, `BuyStop`+`BuyLimit` para posições vendidas).
4. `Command` é automaticamente redefinido para `Wait` depois que a ação é processada para evitar execuções duplicadas acidentais.

### Gerenciando uma posição existente
- Defina novos níveis de preços (use `0` para manter o valor atual) e mude `Command` para `Modify`. A estratégia cancela as ordens de proteção anteriores e as substitui por novas que correspondam aos preços atualizados.
- Mude `Command` para `Close` para liquidar a posição ativa no mercado e cancelar quaisquer ordens de proteção.

## Lógica de gestão de risco
- **AccountReserve** – mantém intacta uma fração do patrimônio máximo. A negociação é bloqueada enquanto o patrimônio disponível (`equity - peak_equity × (1 - AccountReserve)`) for menor que o orçamento de risco solicitado.
- **OrderReserve** – fração do patrimônio atual alocada para a próxima negociação. O orçamento é transformado em tamanho de contrato usando a distância do stop e o valor do tick do instrumento (`PriceStep` × `StepPrice`).
- Se o volume calculado ficar abaixo de `MinVolume` ou violar `VolumeStep`, o comando será rejeitado e um aviso será gravado no log.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `Command` | `Wait` | Comando manual para executar (`Buy`, `Sell`, `Modify`, `Close`). Retorna automaticamente para `Wait` após o manuseio. |
| `StopPrice` | `0` | Nível absoluto de stop-loss. Deve estar abaixo do preço de entrada para posições compradas e acima do preço de entrada para posições vendidas. |
| `TakePrice` | `0` | Nível absoluto de lucro. Deve estar acima do preço de entrada para posições compradas e abaixo do preço de entrada para posições vendidas. |
| `AccountReserve` | `0.20` | Fração do patrimônio líquido mantida como reserva. Valores mais altos exigem uma almofada maior antes que novas negociações sejam aceitas. |
| `OrderReserve` | `0.04` | Fração do patrimônio arriscado por negociação. Usado para calcular o tamanho do contrato a partir da distância de parada. |
| `CandleType` | `1 minute` período de tempo | Série de velas usada para observar os preços mais recentes para validação e registro. |

## Notas e limitações
- Apenas uma posição é suportada por vez, correspondendo ao design original do consultor especialista.
- Comandos que violam a distância mínima de preço, reserva de capital ou restrições de volume são ignorados e um aviso é registrado no log de estratégia.
- As ordens de proteção são substituídas a cada modificação ou novo preenchimento para manter os volumes sincronizados com o tamanho real da posição.
- A estratégia depende de dados de mercado precisos para `PriceStep`/`StepPrice`. Instrumentos que não fornecem esses campos não podem ser negociados com segurança nesta porta.
