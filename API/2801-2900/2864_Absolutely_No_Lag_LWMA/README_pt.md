# Estratégia LWMA Absolutamente Sem Atraso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia replica o assessor especialista do MetaTrader **Exp_AbsolutelyNoLagLwma** aplicando uma dupla média móvel ponderada (LWMA) aos dados de velas. A saída do indicador é codificada por cores: verde (2) para uma inclinação ascendente, cinza (1) para plano, e magenta (0) para uma inclinação descendente. As decisões de trading baseiam-se em transições entre esses estados de cor. A implementação no StockSharp usa a API de alto nível, assina velas de timeframe e envia ordens de mercado de acordo com a direção da tendência detectada.

## Lógica de trading
### Pipeline do indicador
1. Selecionar a série de preços desejada definida pelo parâmetro *Tipo de preço*.
2. Aplicar uma média móvel ponderada (LWMA) com o *Comprimento LWMA* configurado.
3. Suavizar o resultado com um segundo LWMA do mesmo comprimento.
4. Comparar o valor LWMA suavizado com o valor anterior para classificar a direção da inclinação:
   - **2 (tendência de alta)** – o valor atual é maior que o valor anterior.
   - **1 (neutro)** – o valor atual é igual ao valor anterior.
   - **0 (tendência de baixa)** – o valor atual é menor que o valor anterior.

### Avaliação de sinais
- Apenas velas completas são processadas. O parâmetro *Barra de sinal* desloca a avaliação do sinal para velas históricas (1 = vela terminada anterior, 2 = a vela antes dessa, etc.). A estratégia também lembra a cor da barra que precede a vela de sinal selecionada para evitar entradas duplicadas.
- **Transição altista**: a vela de sinal selecionada é cor **2** e a vela anterior não é **2**. Isso abre comprados (se habilitado) e fecha vendidos existentes.
- **Transição baixista**: a vela de sinal selecionada é cor **0** e a vela anterior não é **0**. Isso abre vendidos (se habilitado) e fecha comprados existentes.

### Gestão de posições
- As ordens são executadas com ordens de mercado. O volume solicitado é `Volume + |Position|` quando se inverte a direção para que a posição oposta seja fechada automaticamente.
- Os sinais de saída podem ser alternados independentemente das entradas, permitindo comportamento somente de sinal ou somente de saída.
- `StartProtection()` é ativado para engajar a lógica de proteção comum do StockSharp assim que a estratégia começa.

## Parâmetros
- **Comprimento LWMA** – comprimento dos dois LWMAs usados para suavização.
- **Tipo de preço** – fonte de preço que alimenta o LWMA (fechamento, abertura, máximo, mínimo, mediano, típico, ponderado, simplificado, quarto, variações de seguimento de tendência, ou preço Demark).
- **Barra de sinal** – número de velas terminadas atrás usadas para avaliação de sinais.
- **Habilitar entradas compradas** – permite abrir posições compradas em transições altistas.
- **Habilitar entradas vendidas** – permite abrir posições vendidas em transições baixistas.
- **Habilitar saídas compradas** – fecha posições compradas quando o indicador se torna baixista.
- **Habilitar saídas vendidas** – fecha posições vendidas quando o indicador se torna altista.
- **Tipo de vela** – timeframe da assinatura de velas usada pelo indicador.
- **Volume** (propriedade de Strategy incorporada) – tamanho de operação para novas entradas.

## Notas
- O timeframe padrão é 4 horas, correspondendo à configuração do assessor especialista original, mas pode ser ajustado através do parâmetro *Tipo de vela*.
- Nenhuma ordem de take-profit ou stop-loss é colocada automaticamente; os usuários podem combinar a estratégia com os componentes de gerenciamento de risco do StockSharp, se necessário.
- A portação em Python é intencionalmente omitida conforme solicitado.
