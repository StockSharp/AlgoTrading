# Estratégia de reversão média do centro de gravidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia reconstrói o canal do Centro de Gravidade usado pelo especialista MQL4 original, resolvendo uma regressão polinomial nas velas mais recentes. O centro de regressão é calculado a partir da interceptação do ajuste de mínimos quadrados, enquanto a largura da banda é derivada do desvio padrão dos preços de fechamento no mesmo horizonte retrospectivo. A banda inferior é igual ao centro de regressão menos o desvio escalonado, reproduzindo o buffer `stdl` acessado no robô de origem.

Durante o processamento ao vivo, o algoritmo mantém uma fila contínua de fechamentos com o comprimento definido pelo parâmetro **Bars Back**. Cada vela finalizada aciona um recálculo dos coeficientes de regressão através da eliminação gaussiana no sistema de equações normais. Isso evita o armazenamento de históricos completos de velas, mas produz a mesma geometria de canal que o indicador personalizado. Se a matriz ficar mal condicionada, a atualização será ignorada, evitando decisões de negociação instáveis.

A lógica de negociação reflete o especialista original: quando o mínimo da vela atual permanece acima da faixa de desvio inferior (`lowerBand < Low` na notação MQL), a estratégia considera isso um salto de reversão à média. Se nenhuma posição longa estiver aberta, qualquer exposição curta é fechada e uma ordem de compra de mercado é emitida utilizando o volume da estratégia. Os valores inferior, superior e central mais recentes são expostos por meio de propriedades somente leitura para gráficos ou diagnósticos.

A gestão de riscos é opcional. **Distância de Stop Loss** e **Distância de Take Profit** são especificadas em unidades de preço absoluto. Quando diferente de zero, a estratégia registra o preço de entrada da posição longa ativa e verifica os extremos da vela para determinar se uma meta de stop ou lucro foi atingida. Se nenhum parâmetro for fornecido, a posição poderá ser gerenciada manualmente ou por módulos externos.

### Parâmetros
- **Tipo de vela** – período de assinatura da vela que alimenta a regressão.
- **Bars Back** – número de barras históricas usadas para calcular o canal de regressão (padrão 125).
- **Grau Polinomial** – grau da regressão polinomial (padrão 2) que rege a curvatura do canal.
- **Std Multiplier** – multiplicador aplicado ao desvio padrão na formação do envelope (padrão 1).
- **Distância de Stop Loss** – compensação opcional de stop loss longo em unidades de preço (o padrão 0 desativa-o).
- **Take Profit Distance** – compensação opcional de long takeprofit em unidades de preço (o padrão 0 desativa-o).

A estratégia opera apenas em velas concluídas, utiliza ordens de mercado para entradas e não realiza vendas a descoberto automáticas porque o ramo de venda do especialista original foi comentado.
