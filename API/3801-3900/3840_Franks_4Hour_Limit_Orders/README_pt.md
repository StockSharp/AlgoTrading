# Estratégia de pedidos com limite de 4 horas da Franks
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de pedidos com limite de 4 horas da Franks** transporta o MetaTrader 4 consultor especialista de `MQL/7684/Franks_4hour_limit_orders.mq4` para o StockSharp API de alto nível. O EA original combina as ideias da Tela Tripla de Alexander Elder: ele avalia o impulso em um gráfico de quatro horas usando o histograma MACD (OsMA) junto com o Índice de Força e, em seguida, coloca ordens de limite contrárias em torno dos extremos da vela anterior. A implementação StockSharp mantém essa lógica de múltiplos indicadores enquanto segue as diretrizes do repositório (guias, API de alto nível, sem coleções personalizadas) e adiciona extensos comentários embutidos em inglês para maior clareza.

## Lógica de negociação
1. **Fonte de dados** – A estratégia assina um tipo de vela configurável cujo padrão é velas de quatro horas. Todos os cálculos são realizados apenas em velas concluídas para corresponder ao comportamento do especialista MT4.
2. **Indicadores** – São utilizados dois indicadores gerenciados:
   - `MovingAverageConvergenceDivergenceSignal(12, 26, 9)` fornece a linha MACD e a linha de sinal. A diferença recria o histograma OsMA usado no EA.
   - `ForceIndex(24)` mede a força da vela anterior. Apenas os valores finais dos indicadores são considerados.
3. **Contexto histórico** – O EA requer duas velas concluídas para determinar as inclinações do indicador. A porta armazena os valores OsMA anteriores, o valor anterior do Índice de Força e o máximo/mínimo da vela anterior para espelhar esse requisito.
4. **Configuração de venda** – Quando o histograma OsMA aumenta (`OsMA[1] > OsMA[2]`) e o valor anterior do Índice de Força é negativo, o robô planeja uma ordem de limite de venda contrária:
   - O preço base é a máxima da vela anterior mais um ponto.
   - Um buffer de segurança de 16 pips (configurável) é aplicado em relação à oferta atual. O preço-alvo torna-se o máximo entre o preço base e `Bid + buffer`.
   - Os preços stop-loss e take-profit são alinhados à etapa de preço do instrumento usando as distâncias de pip configuradas (35 pips e 150 pips por padrão).
5. **Configuração de compra** – Quando o histograma OsMA diminui (`OsMA[1] < OsMA[2]`) e o Índice de Força anterior é positivo, a estratégia prepara uma ordem com limite de compra abaixo do mercado:
   - O preço base é o mínimo da vela anterior menos um ponto.
   - O algoritmo impõe o mesmo buffer de 16 pips em relação à oferta atual, escolhendo o mínimo entre o preço base e `Ask - buffer`.
6. **Manutenção de ordem pendente** – Se a inclinação OsMA mudar na direção oposta antes da execução, a ordem pendente correspondente será cancelada. Quando um lado é preenchido, a ordem pendente oposta é removida para evitar dupla exposição.
7. **Gerenciamento de posição** – Após a execução, o preço de preenchimento é armazenado e os níveis pré-calculados de stop-loss e take-profit são ativados. A estratégia também implementa um trailing stop baseado em pip (30 pips por padrão) que move o stop de proteção apenas na direção favorável quando o preço avança além da entrada mais a distância de trailing.
8. **Saídas** – As ordens de proteção são monitoradas em cada vela concluída. Uma posição longa é fechada se a mínima da vela tocar o stop ou a máxima da vela atingir o alvo. As posições curtas usam as regras espelhadas.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `OrderVolume` | 1 | Volume fixo usado para ordens com limite pendentes. |
| `StopLossPips` | 35 | Distância, em pips, entre o preço de entrada e o stop de proteção. |
| `TakeProfitPips` | 150 | Distância, em pips, entre o preço de entrada e o nível de take-profit. |
| `TrailingStopPips` | 30 | Distância, em pips, para o trailing stop que garante lucros quando o preço se move o suficiente. |
| `EntryBufferPips` | 16 | Gap mínimo, em pips, entre o preço de mercado atual e a ordem pendente. |
| `PipSize` | 0,0001 | Tamanho do pip usado para conversões de preços; o padrão é 0,0001, mas pode ser alinhado com símbolos exóticos. |
| `CandleType` | Período de 4h | Série de velas processada pela estratégia. |

## Arquivos
- `CS/Franks4HourLimitOrdersStrategy.cs` – Implementação principal em C# com comentários detalhados em inglês.
- `README.md` – Esta descrição em inglês do algoritmo.
- `README_ru.md` – Documentação russa.
- `README_zh.md` – documentação chinesa.

## Notas de implementação
- A estratégia depende exclusivamente de API de alto nível (`SubscribeCandles`, ligações de indicadores e auxiliares de ordem de conveniência).
- Todos os cálculos de preços estão alinhados com a etapa de preço do instrumento para evitar níveis inválidos.
- As variáveis de estado armazenam apenas os dados históricos necessários, obedecendo à regra do repositório que proíbe coleções personalizadas.
- O gerenciamento de stop-loss, take-profit e trailing stop são realizados dentro da rotina de processamento de velas para emular o comportamento móvel do MT4.
