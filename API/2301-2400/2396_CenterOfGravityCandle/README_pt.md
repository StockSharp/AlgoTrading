# Estratégia de Vela de Centro de Gravidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o especialista MetaTrader "Exp_CenterOfGravityCandle" usando a API de alto nível do StockSharp. O especialista opera velas sintéticas geradas pelo indicador Center of Gravity Candle. Cada vela sintética é construída aplicando o cálculo do Centro de Gravidade de John Ehlers aos preços de abertura, máximo, mínimo e fechamento, e suavizando os resultados com uma média móvel configurável. A cor da vela sintética (altista, baixista ou neutra) é o único sinal de negociação.

## Lógica do indicador

1. Cada vela de mercado recebida é processada após estar completamente fechada.
2. Para cada componente de preço (abertura, máximo, mínimo, fechamento) a estratégia calcula duas médias móveis: uma MA simples e uma MA ponderada linearmente com o período definido por **Period**.
3. O produto dessas duas médias é dividido pelo passo de preço do instrumento e suavizado com o método configurado (**Ma Method**) e comprimento (**Smooth Period**).
4. O máximo e mínimo sintéticos são forçados a incluir a abertura/fechamento sintéticos para que os corpos das velas permaneçam consistentes com a implementação MetaTrader.
5. A cor da vela é determinada comparando a abertura e o fechamento sintéticos: abertura abaixo do fechamento = altista (cor 2), abertura acima do fechamento = baixista (cor 0), caso contrário neutro (cor 1).

## Regras de negociação

1. A estratégia mantém um histórico rotativo de cores de velas sintéticas e inspeciona a barra definida por **Signal Bar** (padrão = barra finalizada anterior).
2. Quando a vela sintética inspecionada se torna altista e a vela anterior não era altista:
   - Fechar qualquer posição vendida existente se **Enable Sell Close** for `true`.
   - Abrir uma nova posição comprada se **Enable Buy Open** for `true`.
3. Quando a vela sintética inspecionada se torna baixista e a vela anterior não era baixista:
   - Fechar qualquer posição comprada existente se **Enable Buy Close** for `true`.
   - Abrir uma nova posição vendida se **Enable Sell Open** for `true`.
4. As entradas de mercado usam o volume calculado a partir de **Money Management** e **Margin Mode**. Valores negativos para **Money Management** são tratados como tamanho de lote fixo. Para modos baseados em perdas, o algoritmo aproxima o risco por trade usando a distância de stop-loss configurada.
5. `StartProtection` é ativado para colocar automaticamente ordens de take-profit e stop-loss de acordo com as distâncias **Take Profit** e **Stop Loss** expressas em passos de preço.

## Parâmetros

- **Money Management** – fração do valor da conta usada para derivar o volume da ordem (valores negativos = lote fixo). Otimizável.
- **Margin Mode** – interpretação do parâmetro de gestão de dinheiro (baseado em equity, baseado em saldo, baseado em perda ou lote fixo).
- **Stop Loss** – distância do stop-loss em passos de preço. Usado tanto para ordens de proteção quanto para dimensionamento de posição baseado em risco.
- **Take Profit** – distância do take-profit em passos de preço. Aplicado via `StartProtection`.
- **Open Long / Open Short** – permitir abrir posições compradas/vendidas em seus respectivos sinais.
- **Close Long / Close Short** – permitir fechar posições compradas/vendidas quando o sinal oposto aparece.
- **Candle Type** – período das velas usadas para o cálculo do indicador.
- **Center of Gravity Period** – período base para as médias móveis simples e ponderada linearmente. Otimizável.
- **Smoothing Period** – comprimento da média móvel de suavização aplicada às velas sintéticas. Otimizável.
- **Smoothing Method** – tipo de média móvel usado na etapa de suavização (SMA, EMA, SMMA ou LWMA).
- **Signal Bar** – índice da vela sintética usada para gerar sinais (0 = atual, 1 = anterior, etc.).

## Notas

- O cálculo do indicador é implementado em C# para reproduzir a lógica original do MetaTrader, evitando buffers manuais ou coleções históricas.
- O cálculo do volume usa informações do portfólio do StockSharp e pode diferir ligeiramente dos resultados do MetaTrader devido a diferenças de plataforma.
- A estratégia opera inteiramente em velas terminadas e nunca negocia em barras parciais.
