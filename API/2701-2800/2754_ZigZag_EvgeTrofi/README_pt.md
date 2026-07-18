# Estratégia ZigZag EvgeTrofi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia ZigZag EvgeTrofi porta o clássico assessor especialista do MetaTrader para a API de alto nível do StockSharp. Ela observa o swing mais recente detectado por um processo de estilo ZigZag e reage rapidamente enquanto o pivô ainda está fresco.

## Conceito

* O advisor original analisa o primeiro ponto não-nulo do buffer ZigZag e decide se o último swing confirmado foi um máximo ou um mínimo.
* Um swing máximo gera uma entrada comprada por padrão. Ativar **SignalReverse** inverte a lógica.
* As posições são abertas apenas enquanto o novo pivô é considerado recente. O parâmetro **Urgency** limita o número de barras após um pivô quando as operações podem ser iniciadas.
* As posições existentes na direção oposta são achatadas imediatamente antes de novas ordens serem colocadas. A estratégia pode escalar na mesma direção em barras consecutivas enquanto a janela de urgência está aberta.

Este port mantém o comportamento contrário: novos máximos acionam operações compradas enquanto mínimos frescos acionam vendidos, imitando a configuração original.

## Como funciona

1. Dois indicadores móveis (`Highest` e `Lowest`) aproximam a lógica de profundidade ZigZag do MetaTrader.
2. Sempre que o preço imprime um novo extremo acima/abaixo dessas bandas e o movimento excede **Deviation** (em passos de preço), um pivô é registrado.
3. O algoritmo rastreia quantas barras passaram desde o pivô. Assim que o contador excede **Urgency**, o sinal expira.
4. Em cada vela fechada durante a janela ativa, a estratégia entra usando `VolumePerTrade`. A exposição oposta é fechada primeiro, para que os giros de posição aconteçam de forma limpa.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `Depth` | 17 | Janela em barras para olhar para trás em busca de swing máximos/mínimos. Espelha a entrada de profundidade do ZigZag. |
| `Deviation` | 7 | Deslocamento mínimo de preço em pontos (multiplicado pelo passo de preço do símbolo) necessário para aceitar um novo pivô. |
| `Backstep` | 5 | Barras que devem decorrer antes que o indicador possa mudar para a direção de pivô oposta. |
| `Urgency` | 2 | Número máximo de barras após o pivô quando as entradas são permitidas. |
| `SignalReverse` | `false` | Inverte o mapeamento de máximos/mínimos para sinais comprados/vendidos. |
| `CandleType` | Velas de 5 minutos | Período usado para a análise. Ajuste ao gráfico que deseja espelhar. |
| `VolumePerTrade` | 0.10 | Tamanho da ordem enviada em cada entrada. Corresponde à entrada de lotes original. |

## Notas de trading

* A lógica **não** inclui stops ou alvos. O controle de risco deve ser adicionado via overlays ou configurações de portfólio se necessário.
* Como o sistema pode adicionar a uma posição em cada barra dentro da janela de urgência, o tamanho da posição pode crescer rapidamente em tendências fortes.
* Use profundidades maiores em símbolos voláteis para evitar pivôs excessivos. Profundidades menores tornam a estratégia mais reativa, mas mais ruidosa.
* Quando **SignalReverse** é true, o comportamento se torna seguimento de rompimento: swing máximos acionam vendidos e swing mínimos acionam comprados.

## Arquivos

* `CS/ZigZagEvgeTrofiStrategy.cs` – Implementação em C# da estratégia.
* A versão em Python não é fornecida intencionalmente.
