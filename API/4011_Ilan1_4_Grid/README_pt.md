# Estratégia de grade de cesta Ilan 1.4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Ilan 1.4 é um sistema clássico de grade de média. A estratégia convertida assina uma única série de velas e abre uma posição inicial de mercado com base na direção das duas últimas velas concluídas: se o fechamento mais recente estiver abaixo do mais antigo, a cesta começa com uma venda, caso contrário, abre uma compra. Quando o preço se move contra a cesta ativa pelo **Pip Step** configurado, a estratégia opcionalmente adiciona uma nova posição na mesma direção e recalcula o preço de entrada médio ponderado.

Todas as negociações dentro da cesta são executadas com ordens de mercado. Quando o preço de fechamento atinge o preço médio de entrada mais a distância **Take Profit**, toda a cesta é fechada. Um trailing stop, um stop loss fixo, uma parada de emergência baseada em ações e uma salvaguarda de vida útil máxima reproduzem os blocos de proteção do especialista MetaTrader original.

## Regras de negociação
1. Aguarde a próxima vela finalizada e avalie os dois últimos fechamentos.
2. Se não houver exposição, abra uma cesta longa quando o último fechamento for superior ao anterior e uma cesta curta caso contrário.
3. Acompanhe o último preço de preenchimento e o preço médio ponderado de entrada da cesta ativa.
4. Quando **Use Add** estiver ativado e o preço se mover contra a posição em **Pip Step** pontos, calcule o próximo tamanho de lote e abra uma negociação de mercado adicional. Se **Fechar antes de adicionar** estiver ativado, a cesta existente será fechada primeiro e reaberta com o volume dimensionado.
5. Recalcular o preço médio de entrada após cada preenchimento. A cesta é liquidada quando o preço atinge o nível médio de lucro ou quando qualquer uma das regras de risco é acionada.
6. Assim que uma cesta é fechada, a lógica prepara imediatamente um novo sinal usando os dois últimos fechamentos da vela.

## Modos de gerenciamento de dinheiro
O parâmetro **Money Management** reproduz a opção `MMType` original:
- **Fixo** – cada novo pedido usa o **Volume Inicial** configurado.
- **Geométrico** – pedidos subsequentes multiplicam o volume base por `LotExponent^n`, onde `n` é igual ao número atual de negociações abertas.
- **RecoverLastLoss** – após uma cesta perdida, a próxima posição utiliza o volume da última negociação fechada multiplicado por **Lot Exponent**; cestas lucrativas redefinem o volume de volta ao valor base.

Os volumes de negociação são arredondados de acordo com **Dígitos de Volume** e a etapa de volume do título. Quando o arredondamento produziria zero, o volume de entrada não arredondado será usado.

## Controles de risco
- **Take Profit** – fecha toda a cesta quando o preço atinge o preço médio de entrada ± pontos configurados.
- **Stop Loss** – fecha a cesta quando o preço se move contra o preço médio de entrada pelo número especificado de pontos.
- **Use Trailing Stop** com **Trail Start** e **Trail Stop** – ativa um nível de trailing quando a cesta ganha pontos suficientes; a compensação final segue o preço para proteger os lucros.
- **Usar Equity Stop** com **% de Risco de Ações** – monitora o valor do portfólio e fecha a cesta quando a perda flutuante excede a porcentagem escolhida do pico de ações registrado.
- **Usar tempo limite** com **Max Open Hours** – fecha a cesta à força quando ela permanece aberta por mais tempo do que o número permitido de horas.

## Parâmetros
- **Tipo de vela** – intervalo de tempo usado para gerar sinais de negociação.
- **Volume Inicial** – tamanho do lote inicial para uma nova cesta.
- **Dígitos de Volume** – precisão usada ao arredondar volumes calculados.
- **Gerenciamento de dinheiro** – modo de cálculo de volume (`Fixed`, `Geometric`, `RecoverLastLoss`).
- **Expoente do Lote** – multiplicador aplicado pelos esquemas geométrico e de recuperação.
- **Fechar antes de adicionar** – feche todas as negociações abertas antes de colocar a próxima ordem de média.
- **Use Adicionar** – habilite ou desabilite completamente os pedidos médios.
- **Pip Step** – movimento adverso mínimo (em etapas de preço) antes de adicionar uma nova negociação.
- **Take Profit** – meta de lucro a partir do preço médio de entrada.
- **Stop Loss** – excursão adversa máxima permitida em relação ao preço médio de entrada.
- **Use Trailing Stop / Trail Start / Trail Stop** – configuração de trailing-stop.
- **Max Trades** – número máximo de negociações médias permitidas dentro de uma cesta.
- **Use Equity Stop / Equity Risk %** – parâmetros de proteção contra perdas flutuantes.
- **Use Timeout / Max Open Hours** – controle de vida útil para cada cesta.

## Notas de conversão
- MetaTrader auxiliares de ordens pendentes foram substituídos por ordens diretas de mercado porque a lógica de média sempre era executada imediatamente no código original.
- O bloco final agora funciona na cesta agregada em vez de modificar cada pedido separadamente; as distâncias de disparo correspondem aos padrões originais.
- O patrimônio do portfólio é monitorado por meio do objeto de portfólio StockSharp para emular a rotina de parada de patrimônio do especialista.
- As médias de posição e as estatísticas da cesta são calculadas dentro da estratégia sem armazenar coleções por negociação, respeitando as diretrizes de alto nível API.
