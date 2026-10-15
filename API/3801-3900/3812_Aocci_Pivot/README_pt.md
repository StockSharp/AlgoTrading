# Estratégia AOCCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia AOCCI é uma conversão direta do consultor especialista MetaTrader 4 "AOCCI". Ele combina filtros de impulso e reversão à média usando o Awesome Oscillator (AO) e o Commodity Channel Index (CCI) junto com um pivô diário. A versão convertida funciona no alto nível de StockSharp API e mantém a mesma lógica de proteção do script original.

## Lógica
1. **Preparação de dados**
   - Usa velas intradiárias (padrão 1 hora) para geração de sinal.
   - Usa velas diárias para calcular o pivô do dia anterior concluído (máxima + mínima + fechamento dividido por 3).
   - Rastreia os últimos seis preços de abertura intradiários para detectar grandes lacunas.
2. **Filtro de lacunas**
   - Qualquer diferença de passo único que exceda o limite do *Big Jump Filter* cancela o sinal atual.
   - Qualquer diferença combinada de duas etapas que exceda o limite do *Filtro de Salto Duplo* também cancela o sinal.
3. **Verificações de indicadores**
   - AO deve ser maior que zero e CCI deve ser não negativo na barra atual.
   - Pelo menos um dos seguintes itens deve ser verdadeiro na barra anterior: AO abaixo de zero, CCI igual ou inferior a zero ou preço abaixo do pivô.
4. **Filtro direcional**
   - O preço de fechamento deve permanecer acima do nível pivô.
5. **Pedidos**
   - O consultor especialista original só abre negociações longas porque a condição curta duplica a lógica longa. A conversão mantém esse comportamento.
   - As ordens de mercado usam o *Volume de Ordens* configurado.
6. **Proteção**
   - O stop loss e o take-profit iniciais são expressos em etapas de preço.
   - O trailing stop opcional estreita o stop quando o preço se move a favor da posição em pelo menos a distância final.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CciPeriod` | Período para o Commodity Channel Index (padrão 55). |
| `SignalCandleOffset` | Compensação adicional aplicada ao fazer referência a velas diárias históricas (padrão 0). |
| `StopLossPoints` | Distância de stop-loss em etapas de preço. |
| `TakeProfitPoints` | Distância de lucro em etapas de preço. |
| `TrailingStopPoints` | Distância do trailing stop em etapas de preço (0 desativa o trailing). |
| `BigJumpPoints` | Gap máximo permitido de abertura de barra única expresso em etapas de preço. |
| `DoubleJumpPoints` | Gap máximo permitido combinado de duas barras expresso em etapas de preço. |
| `OrderVolume` | Volume utilizado no envio de ordens de mercado. |
| `CandleType` | Tipo de vela intradiária (barras padrão de uma hora). |
| `DailyCandleType` | Tipo de vela diária usado para cálculo de pivô. |

## Notas de uso
- A estratégia requer assinaturas de dados intradiárias e diárias.
- A etapa de preço (tamanho do tick) do título selecionado é usada para traduzir parâmetros de risco baseados em pontos em preços reais.
- O gerenciamento de trailing stop é aplicado em velas concluídas, refletindo o comportamento do EA original.
- Como a versão original MQL4 nunca aciona negociações curtas, a conversão mantém intencionalmente o mesmo conjunto de regras.
