# Estratégia de pausa MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o comportamento dos especialistas MetaTrader "M.A break mt5 buy" e "M.A break mt5 sell" combinando ambas as direções de ruptura em uma única implementação StockSharp. Ele observa uma série de velas configuráveis, analisa várias médias móveis exponenciais e confirma uma forte vela de impulso antes de abrir negociações. As posições são gerenciadas através de paradas de proteção fixas e alvos medidos em pips.

## Lógica de negociação

1. **Confirmação de tendência.** Dois pares EMA (rápido vs. lento) devem estar alinhados na direção de negociação na vela concluída. Para posições compradas, ambas as médias rápidas devem estar acima de suas contrapartes lentas; para shorts as relações são invertidas. A vela anterior aberta também deve estar no lado correto de um filtro EMA dedicado.
2. **Medição de faixa de silêncio.** Um número configurável de velas anteriores (excluindo a vela de impulso mais recente) define o período de "silêncio". Seu intervalo mais alto é comparado com um limite mínimo de pip.
3. **Detecção de impulso.** A última vela finalizada deve se expandir em pelo menos `ImpulseStrength` vezes a faixa de silêncio. Os limites de tamanho da vela em pips podem ser aplicados para ignorar movimentos incomumente pequenos ou grandes.
4. **Modelo de castiçal.** A vela de impulso deve apresentar uma estrutura de pavio específica:
   - Negociações longas: corpo de alta, pavio superior não excedendo `BullUpperWickPercent` do intervalo da vela e pavio inferior pelo menos `BullLowerWickPercent` do intervalo.
   - Negociações curtas: corpo de baixa, pavio superior de pelo menos `BearUpperWickPercent` e pavio inferior não maior que `BearLowerWickPercent` do intervalo.
5. **Condição de pullback.** O impulso de baixa (para posições compradas) ou alta (para vendas) deve testar um EMA adicional para garantir que o rompimento surgiu de uma retração.
6. **Controle de posição.** Somente uma posição líquida é permitida. A estratégia fecha o lado oposto antes de entrar numa nova negociação e nunca abre uma posição contra o filtro de tendência.
7. **Gerenciamento de saída.** Os níveis de stop-loss e take-profit são calculados em pips a partir do preço de entrada. Cada vela finalizada verifica se o preço atingiu os níveis de proteção e sai de acordo.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| **Tipo de vela** | Série de velas primárias usada para todos os cálculos. |
| **MA rápido 1/MA lento 1** | Períodos do primeiro par EMA que define a tendência primária. |
| **MA rápido 2/MA lento 2** | Períodos do par secundário EMA usados como filtro de tendência adicional. |
| **Abrir filtro MA** | Período EMA que filtra o preço de abertura da vela anterior. |
| **Retração MA** | EMA período cujo valor deve ser tocado pelo pavio de impulso. |
| **Bares tranquilos** | Número de velas históricas usadas para medir a faixa tranquila do mercado. |
| **Faixa Silenciosa (pips)** | Faixa mínima de pip necessária nas velas silenciosas antes de considerar um rompimento. |
| **Multiplicador de impulso** | Relação mínima entre o tamanho da vela de impulso e a faixa silenciosa. |
| **Tamanho mínimo/máximo da vela (pips)** | Limites opcionais para a faixa de velas de impulso. Zero desativa o respectivo limite. |
| **% do pavio superior do touro /% do pavio inferior do touro** | Filtros de formato para a vela de impulso de alta, expressos como porcentagens da faixa da vela. |
| **% de pavio superior do urso/% de pavio inferior do urso** | Filtros de forma para a vela de impulso de baixa. |
| **Volume** | Tamanho do pedido em lotes usados para entradas longas e curtas. |
| **Stop-Loss (pips)** | Distância até o stop de proteção medida a partir do preço de entrada. Zero desativa a parada. |
| **Realização de lucro (pips)** | Distância até a meta de lucro. Zero desativa o alvo. |
| **Ativar Longo/Ativar Curto** | Alterne a negociação de breakout em cada direção de forma independente. |

## Notas de uso

- Configure a série de velas para corresponder ao período usado pelo especialista original (por exemplo, M5 ou H1). O padrão é um período de 5 minutos.
- A estratégia armazena apenas o histórico recente necessário para o cálculo do intervalo silencioso, evitando o uso desnecessário de memória.
- Os preços de entrada são aproximados pelo fechamento da vela de impulso, que corresponde ao comportamento original MetaTrader de colocar ordens de mercado no início da próxima barra.
- Os níveis de stop-loss e take-profit são avaliados em cada vela concluída. Se ambos os níveis forem atingidos na mesma barra, o stop terá prioridade, refletindo o tratamento conservador usado nos especialistas da fonte.
- Ativar apenas uma direção reproduz os consultores especializados originais de "compra" ou "venda", enquanto deixar ambos os botões ativos permite uma negociação de breakout simétrica.

## Detalhes da conversão

- Ambos os arquivos MQ5 originais foram codificados em UTF-16 e construídos a partir de blocos gerados pelo mecanismo FXD. Cada bloco foi traduzido em lógica C# explícita.
- As comparações de EMA e modelos de velas seguem as mesmas mudanças da versão MetaTrader (`Shift = 1`), o que significa que a estratégia sempre avalia velas totalmente fechadas.
- A lógica de parada virtual e os rótulos gráficos dos scripts MQ5 foram omitidos intencionalmente porque não influenciam a colocação de pedidos.

## Teste

Compile a solução por meio de `AlgoTrading.sln` ou execute a estratégia dentro do testador de estratégia StockSharp. Ajustar a etapa de preço do instrumento se os metadados de segurança não possuírem esta informação; a implementação recorre a `0.0001` para emular valores comuns de pip FX.
