# Estratégia do Código da Sorte
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Lucky Code é um scalper de curto prazo convertido do consultor especialista MetaTrader "Lucky_code" original. A estratégia observa os extremos do spread e reage quando o melhor pedido salta acima ou o melhor lance cai abaixo da cotação anterior por uma distância configurável. Todas as negociações são fechadas de forma agressiva: os lucros são obtidos imediatamente quando o preço sobe favoravelmente, enquanto as perdas são reduzidas quando uma excursão adversa viola um limite de proteção.

## Dados e execução

- **Dados de mercado**: requer um fluxo constante de cotações de Nível 1 para ler os melhores valores de compra e venda mais recentes.
- **Tipos de ordens**: usa ordens de mercado para cada entrada e saída para espelhar a execução baseada em ticks da versão MQL.
- **Modo de posição**: suporta contas de compensação e de hedge. Vários preenchimentos se acumulam em uma única posição líquida que é gerenciada como um bloco.

## Parâmetros

- **Pontos de deslocamento** – número mínimo de pontos (pips) entre cotações consecutivas que desbloqueia uma nova entrada. Valores mais altos reduzem a frequência comercial e a sensibilidade ao ruído.
- **Pontos limites** – distância adversa máxima permitida antes do fechamento forçado das posições. O valor é convertido em unidades de preço com o tamanho do tick do instrumento.

## Lógica de negociação

1. **Inicialização**
   - Converte parâmetros baseados em pontos em compensações de preços reais usando o tamanho do tick de segurança.
   - Assina os dados do Nível 1 e redefine os buffers internos para o último lance e venda visto.
2. **Regras de entrada**
   - Quando a melhor venda avança pelo menos o deslocamento configurado acima da venda anterior, a estratégia abre uma posição curta (correspondendo ao comportamento original EA que vende após picos ascendentes).
   - Quando a melhor oferta cai pelo menos na mesma mudança da oferta anterior, a estratégia abre uma posição longa para capturar a recuperação.
3. **Dimensionamento de volume**
   - Começa na propriedade da estratégia `Volume`.
   - Se o valor do portfólio estiver disponível, o tamanho é aumentado para `round(Equity / 10,000, 1)` lotes, emulando o dimensionamento baseado em margem de MetaTrader.
4. **Regras de saída**
   - A exposição longa é fechada imediatamente quando o lance excede o preço médio de entrada ou o pedido desce pelo limite de perda configurado.
   - A exposição curta é encerrada quando o preço de venda cai abaixo do preço de entrada ou o lance sobe acima dele até o limite de perda.

## Notas de implementação

- A estratégia reage a cada atualização de cotação, portanto, considere limitar feeds barulhentos ou aumentar o parâmetro de mudança em ambientes de produção.
- Como as ordens de mercado são usadas tanto para abertura quanto para fechamento de negociações, garanta liquidez suficiente para evitar picos de derrapagem durante saltos rápidos nas cotações.
- Controles adicionais de risco em nível de portfólio (stop diário, rebaixamento máximo, etc.) são recomendados ao executar a estratégia ao vivo.
