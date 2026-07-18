# Estratégia Karakatica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia Karakatica é um sistema de acompanhamento de tendências de médio prazo que foi transferido do consultor especialista MetaTrader 4 original "Exp_karakatica". A estratégia é negociada **EUR/USD no período M15** por padrão e usa um mecanismo de sinal personalizado que emula o comportamento do indicador "iKarakatica" original com um modelo cruzado de média móvel. O cruzamento é recalculado em cada barra e o período do sinal é continuamente reotimizado para seguir o regime recente mais lucrativo.

A estratégia entra no mercado com ordens de mercado somente quando nenhuma posição está aberta no momento. As ordens de proteção (stop-loss e take-profit) são anexadas automaticamente por meio do subsistema de proteção StockSharp.

## Lógica de negociação
1. **Geração de sinal** – A estratégia calcula uma média móvel simples (SMA) dos preços de fechamento das velas. Um sinal de alta aparece quando a vela anterior fechou abaixo ou em SMA enquanto a última vela finalizada fechou acima dela. Um sinal de baixa é produzido quando a vela anterior fecha acima ou em SMA e a última vela fecha abaixo dela. Os sinais são sempre avaliados na barra *anterior* concluída para espelhar a implementação MT4 que usou valores shift=1 do indicador `iKarakatica`.
2. **Gerenciamento de posição** –
   - Se aparecer um sinal oposto enquanto uma posição estiver aberta, a posição será fechada imediatamente com uma ordem de mercado.
   - Novas negociações são permitidas somente quando não existe posição e a estratégia não está bloqueada pela fase de otimização. As negociações consecutivas na mesma direção são bloqueadas até que o mercado produza um sinal oposto confirmado.
3. **Dimensionamento do pedido** – O tamanho da posição é derivado do parâmetro `Risk` configurado. O algoritmo converte o risco em um volume desejado com base no valor atual do portfólio e, em seguida, alinha-o com a etapa de volume do instrumento, imitando o método de cálculo de lote do consultor especialista original.
4. **Proteção comercial** – As distâncias de stop-loss e take-profit são definidas em faixas de preço. Eles são traduzidos em preços absolutos multiplicando o valor do ponto pela etapa do preço do instrumento.

## Otimização Adaptativa
O consultor especialista reotimiza continuamente o período do sinal para se adaptar às mudanças no comportamento do mercado:

1. A cada `ReoptimizeEvery` barras, a estratégia lança uma simulação histórica que cobre `OptimizationDepth` barras anteriores.
2. Para cada período candidato no intervalo `[OptimizationStart, OptimizationEnd]` com uma etapa `OptimizationStep`, o backtester simula um modelo simples de cruzamento de média móvel:
   - O simulador acompanha uma posição virtual ativa e atualiza seu lucro sempre que o sinal oposto é acionado.
   - Contadores de lucro separados são mantidos para negociações longas e curtas, além do lucro combinado.
3. Depois de digitalizar todos os candidatos, a estratégia aplica as seguintes regras:
   - Se os lucros longos e curtos forem negativos, a negociação em ambas as direções será desativada até o próximo ciclo de otimização.
   - Se os melhores resultados longos e curtos forem iguais, o melhor período geral será usado e ambas as direções permanecerão habilitadas.
   - Caso contrário, apenas a direção com maior lucro permanece habilitada e o melhor período correspondente é selecionado.

A otimização requer pelo menos `OptimizationDepth + OptimizationEnd + 2` velas concluídas para iniciar. Até que seja coletado histórico suficiente, a estratégia atrasa a negociação.

## Parâmetros
| Nome | Descrição | Padrão | Otimizável |
| ---- | ----------- | ------- | ----------- |
| `Risk` | Porcentagem do valor do portfólio (por 1.000 unidades) que define o volume alvo de pedidos. | 0,5 | Sim |
| `StopLossPoints` | Distância de stop-loss em faixas de preço. | 50 | Sim |
| `TakeProfitPoints` | Distância de lucro em faixas de preço. | 150 | Sim |
| `Period` | Período SMA ativo usado para geração de sinal. Atualizado automaticamente pelo otimizador. | 70 | Sim |
| `OptimizationDepth` | Número de barras históricas usadas para o backtest na amostra. | 250 | Não |
| `ReoptimizeEvery` | Frequência de execuções de otimização medidas em barras acabadas. | 50 | Não |
| `OptimizationStart` | Período mínimo considerado durante a otimização. | 10 | Não |
| `OptimizationStep` | Passo entre os períodos vizinhos. | 5 | Não |
| `OptimizationEnd` | Período máximo considerado durante a otimização. | 150 | Não |
| `CandleType` | Tipo de dados das velas (o padrão é um período de 15 minutos). | Velas de prazo M15 | Não |

## Notas de uso
- A estratégia foi projetada para EUR/USD no período de 15 minutos. Ao migrar para um instrumento diferente, revise o valor do ponto, a etapa de volume e as premissas de spread.
- Certifique-se de que o feed de dados forneça as melhores cotações de compra/venda. Eles são usados ​​para estimar o spread comercial durante o processo de otimização. Quando as cotações não estão disponíveis, o algoritmo recorre a um spread de preço único.
- Como a lógica de otimização requer várias centenas de barras históricas, permita que a estratégia pré-carregue os dados antes de ativar a negociação em tempo real.

## Arquivos
- `CS/KarakaticaStrategy.cs` – StockSharp implementação da estratégia.
- `README.md` – Descrição em inglês (este arquivo).
- `README_ru.md` – Descrição russa.
- `README_zh.md` – descrição chinesa.
