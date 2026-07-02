# Estratégia RDS de Tendências
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Trend RDS é uma estratégia de reversão baseada em sessão originalmente escrita para MetaTrader. Ele procura uma formação de impulso de três barras no início de uma janela de negociação específica e atenua o movimento entrando na direção oposta. A porta StockSharp mantém a lógica original de gerenciamento de dinheiro, incluindo reversão opcional dos sinais, níveis fixos de stop-loss e take-profit, proteção de ponto de equilíbrio e um trailing stop com tamanho de passo ajustável.

## Lógica de negociação
1. **Janela de sinal** – No `Start Time` configurado a estratégia inspeciona até 100 velas fechadas recentemente.
2. **Detecção de padrão** – Procura a primeira sequência de três barras consecutivas onde:
   - Os máximos aumentam enquanto os mínimos aumentam (`High[n] < High[n+1] < High[n+2]` e `Low[n] > Low[n+1] > Low[n+2]`).
   - Os máximos caem enquanto os mínimos caem (`High[n] > High[n+1] > High[n+2]` e `Low[n] < Low[n+1] < Low[n+2]`).
Uma expansão simétrica em ambas as direções é tratada como um conflito e ignorada. A direção do sinal é opcionalmente invertida quando `Reverse Signals` está ativado.
3. **Entradas** – A estratégia envia uma ordem de mercado com o `Trade Volume` configurado se não houver posição aberta. Se a posição oposta ainda estiver aberta, ela será fechada primeiro.
4. **Janela de saída forçada** – Entre `Close Time` e quinze minutos depois, qualquer posição residual é liquidada.
5. **Proteção** – Assim que a posição é aberta, a estratégia registra:
   - Uma ordem de stop-loss e take-profit nas distâncias de pip solicitadas.
   - Um gatilho de ponto de equilíbrio que move o stop para o preço de entrada após atingir `Break-Even (pips)`.
   - Um trailing stop que mantém uma distância de `Trailing Stop (pips)` do preço atual e avança somente após um movimento adicional de `Trailing Step (pips)`.

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| **Volume comercial** | Tamanho da ordem de mercado expresso em lotes ou contratos. |
| **Stop Loss (pips)** | Distância até a parada de proteção. Defina como zero para desativar. |
| **Take Profit (pips)** | Distância até a meta de lucro. Defina como zero para desativar. |
| **Hora de início** | Hora do dia (horário de troca) em que a busca de padrão começa. |
| **Hora de fechamento** | Hora do dia (horário de câmbio) em que todas as negociações abertas são fechadas em 15 minutos. |
| **Sinais Reversos** | Inverte entradas longas e curtas. |
| **Trailing Stop (pips)** | Distância de fuga base. Zero desativa o rastreamento. |
| **Etapa final (pips)** | Movimento extra necessário antes que o trailing stop seja atualizado novamente. |
| **Ponto de equilíbrio (pips)** | Limite de lucro para mover o stop para o preço de entrada. Zero desativa o recurso. |
| **Tipo de vela** | Série de velas utilizadas para a análise. |

## Notas práticas
- A estratégia depende da etapa do preço do instrumento para calcular as distâncias do pip. Certifique-se de que a segurança exponha um valor `PriceStep` ou `MinPriceStep` válido.
- Apenas as velas finalizadas são processadas, portanto o sinal pode aparecer no máximo uma vez por dia por período de tempo.
- As ordens stop e take-profit são atualizadas sempre que o tamanho da posição muda, garantindo que os preenchimentos parciais mantenham uma proteção consistente.
- A lógica de trailing e ponto de equilíbrio é ativada apenas enquanto uma posição está aberta e um preço de entrada válido é conhecido.

## Arquivos
- `CS/TrendRdsStrategy.cs` – StockSharp Implementação da estratégia em C#.
- `README_zh.md` – documentação chinesa.
- `README_ru.md` – Documentação russa.
