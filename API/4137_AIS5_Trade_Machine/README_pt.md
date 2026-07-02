# Máquina comercial AIS5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
AIS5 Trade Machine transporta o MetaTrader 4 consultor especialista `AIS5TM.mq4` para a StockSharp estratégia de alto nível API. O original
programa focado na construção de histogramas de perfil de mercado em dois intervalos de tempo e oferecendo um console de execução semiautomático. O
A versão StockSharp mantém a ideia de destacar zonas de preços fortes e fracas a partir da agregação do volume de ticks e a transforma em um
sistema de breakout automatizado com controle de risco adaptativo baseado no Average True Range (ATR).

A estratégia assina dois fluxos de velas:
* Um **período de perfil** (padrão 15 minutos) que acumula volume para detectar zonas fortes e fracas.
* Um **período de negociação** (padrão 1 minuto) que procura rompimentos confirmados por volume fora dessas zonas.

As posições são protegidas por paradas proporcionais ATR e metas escaláveis. As contrações de volume desencadeiam saídas precoces para imitar o
disciplina de monitoramento encontrada no código MT4.

## Lógica estratégica
### Detecção de zona de volume (período de perfil)
* Cada vela finalizada de período superior atualiza duas médias móveis simples (SMA) do volume de ticks.
* Uma vela é rotulada como **zona forte** quando seu volume excede a média do multiplicador configurável (`Strong Volume Mult`).
O preço de fechamento da vela torna-se o nível forte mais recente.
* Uma vela é rotulada como **zona fraca** quando seu volume cai abaixo da média dividida pelo divisor configurado
(`Weak Volume Divider`). O preço de fechamento dessa vela torna-se o último nível fraco.
* Participam apenas velas acabadas. A estratégia ignora zonas até que o perfil SMA esteja totalmente formado para evitar
sinais durante o período de aquecimento.

### Entradas de breakout (período de negociação)
* O período inferior aguarda que seu volume SMA e o indicador ATR terminem de se formar.
* Uma configuração longa exige que o fechamento exceda o nível forte mais recente pela soma dos **Pontos Base da Zona** e
**Zone Step Points** buffers (convertidos por meio da etapa de preço do instrumento). A vela também deve fornecer um pico de volume relativo
à média intradiária.
* Uma configuração curta reflete a lógica em torno do último nível fraco, exigindo uma quebra além do buffer combinado e confirmando
expansão volumétrica.
* O especialista MT4 original permitia comandos manuais e grades de múltiplas ordens. A porta StockSharp mantém um modelo de posição única, então um
o rompimento só é acionado quando a posição líquida atual é estável.

### Gerenciamento de saída
* Na entrada, a estratégia armazena o preço de preenchimento, calcula um stop de proteção baseado em ATR (ATR multiplicado por `ATR Multiplier` e
fixado pelo buffer da zona base) e define o alvo como a distância de parada multiplicada pelo divisor de volume fraco. Isso mantém
risco e recompensa alinhados com a estrutura de volume.
* Enquanto uma posição está aberta, a estratégia monitora cada vela de negociação concluída:
  * Se o preço atingir a meta de lucro ou o stop de proteção, a posição será achatada imediatamente.
  * Se o volume de ticks contrair abaixo do limite de volume fraco antes de qualquer nível ser atingido, a negociação será fechada mais cedo para evitar
permanecendo em zonas inativas.
* Quando a posição líquida retorna a zero, o estado interno é redefinido, permitindo que o próximo rompimento seja avaliado do zero.

## Parâmetros
* **Vela de Perfil** – tipo de vela que alimenta o perfil de volume (padrão: velas de 15 minutos).
* **Vela de negociação** – período de tempo inferior usado para rompimentos e saídas (padrão: velas de 1 minuto).
* **Volume Lookback** – número de velas para SMAs de volume e período ATR.
* **Strong Volume Mult** – multiplicador acima do volume médio que marca uma zona forte (mapeia para `Parameter.1` em MQL).
* **Divisor de Volume Fraco** – divisor abaixo do volume médio que marca zonas fracas e dimensiona a meta de lucro (mapeia para
`Parameter.2`).
* **ATR Multiplicador** – fator de escala aplicado a ATR ao calcular a distância de parada adaptativa (mapeia para `Parameter.3`).
* **Pontos base da zona** – buffer mínimo em pontos adicionados ao nível da zona antes de verificar os rompimentos (mapeia para `ZoneBasePoints`).
* **Zone Step Points** – buffer de breakout adicional em pontos que amplia a distância da zona antes que as entradas sejam
acionado (mapeia para `ZoneStepPoints`).
* **Volume** – herdado da classe base `Strategy`; define o tamanho da ordem para entradas no mercado.

## Notas adicionais
* A estratégia volta automaticamente para uma etapa de preço padrão de `0.0001` se o título não especificar uma. Isto mantém o
parâmetros baseados em pontos compatíveis com a maioria dos símbolos FX.
* Todos os cálculos dos indicadores dependem de velas finalizadas para corresponder à implementação do MT4 que funcionou em barras totalmente fechadas.
* Ao contrário do EA original, não há painel de controle manual ou carregador de perfil baseado em arquivo. As zonas são reconstruídas puramente a partir de live
dados da vela para manter a porta independente.
* A versão StockSharp não inclui uma tradução Python.
