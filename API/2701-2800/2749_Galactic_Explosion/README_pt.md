# Estratégia de Explosão Galáctica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de Explosão Galáctica reconstrói o especialista de grade original do MetaTrader 5 no StockSharp. Opera em velas finalizadas, usa uma média móvel de longo prazo para definir o viés direcional e implanta uma grade expansiva de ordens. O sistema acumula operações quando o preço permanece em um lado da média móvel e fecha toda a cesta uma vez que um alvo de lucro predefinido é alcançado.

## Lógica de mercado
1. **Filtro direcional** – a estratégia compara o último fechamento de vela com uma média móvel. Quando o preço fecha abaixo da média o viés se torna de alta, e quando o preço fecha acima da média o viés se torna de baixa.
2. **Grade progressiva** – as primeiras oito entradas são tomadas sempre que o viés permite. Após a oitava posição, a distância entre o preço atual e tanto a última quanto a primeira entrada controla se operações adicionais são permitidas.
3. **Controle de espaçamento** – as distâncias são medidas em passos de preço. Se o preço se moveu o suficiente desde a última entrada, a estratégia adicionará à cesta. Dependendo da distância até a primeira entrada, operará imediatamente, pulará três velas, ou pulará seis velas antes de adicionar novamente.
4. **Realização de lucro** – o PnL realizado mais o lucro aberto da cesta é comparado ao alvo de lucro mínimo. Quando o limiar é atingido, todas as posições abertas são fechadas em uma única ordem a mercado.

## Gestão de operações
- **Volume de entrada** – cada operação é executada com o volume de ordem configurado. Quando o sinal muda enquanto mantém uma posição, a estratégia envia uma única ordem que fecha o lado antigo e abre um novo com o volume extra necessário.
- **Rastreamento de posição** – a estratégia mantém o preço médio e o preço de primeira/última entrada para cestas compradas e vendidas de forma independente. Isso permite reproduzir as regras de dimensionamento baseadas em distância do especialista original.
- **Filtro de sessão** – o trading está ativo apenas entre as horas de início e fim configuradas. A lógica usa o tempo de abertura da vela e ignora sinais fora desta janela.
- **Verificação de segurança** – se a janela de trading está mal configurada (por exemplo, a hora de início não é anterior à hora de fim), a estratégia pula o trading e registra um aviso.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| **Order Volume** | Volume usado para cada nova entrada. Este valor também é usado para estimar quantos passos de grade estão atualmente abertos. |
| **Start Hour** | Início da sessão de trading no horário da bolsa. Sinais antes desta hora são ignorados. |
| **End Hour** | Fim da sessão de trading (exclusivo). Sinais após esta hora são ignorados. |
| **Minimal Profit** | Lucro combinado realizado mais não realizado que aciona o fechamento de todas as posições abertas. |
| **Indent After 8th** | Distância mínima (em passos de preço) desde a entrada mais recente após oito operações antes que outra posição possa ser aberta. |
| **Skip 3 Min** | Limite inferior (em passos de preço) para ativar a regra de "pular três velas". |
| **Skip 3 Max** | Limite superior (em passos de preço) que mantém ativa a regra de "pular três velas". |
| **Skip 6 Max** | Limite superior (em passos de preço) que mantém ativa a regra de "pular seis velas". |
| **MA Length** | Comprimento da média móvel simples que define o viés direcional. |
| **Candle Type** | Série de velas assinada pela estratégia. A média móvel e a lógica de grade são executadas neste fluxo de dados. |

## Notas de implementação
- A estratégia usa `SubscribeCandles` com um indicador `SimpleMovingAverage` e processa apenas velas finalizadas.
- As estatísticas de posição são mantidas através de `OnNewMyTrade`, permitindo rastreamento preciso dos preços de primeira e última entrada, bem como preços médios para cestas abertas.
- Os limiares de distância são escalados pelo `PriceStep` do ativo, reproduzindo a configuração original baseada em pips do especialista MT5.
- A implementação evita coleções personalizadas e foca em variáveis de estado escalares para cumprir com as diretrizes do projeto.
